using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using com.faolline.translationsystem;

namespace com.faolline.dialoguesystem
{
    public class DialogueUIAdapter : MonoBehaviour
    {
        [Header("Bindings")]
        [SerializeField] private TranslateDialogueLine line;
        [SerializeField] private List<TranslateDialogueChoiceText> choiceSlots = new List<TranslateDialogueChoiceText>();
        [SerializeField] private GameObject choicesContainer;

        [Header("Avatar mounts")]
        [Tooltip("Emplacement de l'avatar courant (si null, 'avatarRoot' sera utilisé).")]
        [SerializeField] private Transform currentAvatarRoot;
        [Tooltip("Emplacement de l'avatar précédent (optionnel).")]
        [SerializeField] private Transform previousAvatarRoot;
        [Tooltip("Compat: utilisé si 'currentAvatarRoot' est null.")]
        [SerializeField] private Transform avatarRoot;

        [Header("Lifecycle")]
        [SerializeField] private bool destroyAvatarOnHide = true;

        [Header("Transitions (optionnelles)")]
        [SerializeField] private AvatarTransition transition;   // ← plug SO ici
        [SerializeField] private bool waitTransitions = true;   // si true, on attend la fin des coroutines

        // events (si tu préfères driver tes FX côté Manager)
        public event System.Action<GameObject> OnAvatarDemoted;   // courant → previous
        public event System.Action<GameObject> OnAvatarSpawned;   // nouveau courant instancié
        public event System.Action<GameObject> OnAvatarDespawned; // avatar détruit

        // cache speakers
        private readonly Dictionary<string, Speaker> _speakersById = new();

        // avatars runtime
        private GameObject _currentAvatar;
        private GameObject _previousAvatar;

        // meta pour éviter respawn inutiles
        private string _currentSpeakerKey;
        private string _currentExprKey;

        // swap en cours
        private Coroutine _swapCo;

        // ---------- Setup ----------
        public void BindSpeakers(IReadOnlyList<Speaker> speakers)
        {
            _speakersById.Clear();
            if (speakers == null) return;
            foreach (var s in speakers)
            {
                if (!s) continue;
                var id = s.SpeakerName;
                if (!string.IsNullOrEmpty(id) && !_speakersById.ContainsKey(id))
                    _speakersById.Add(id, s);
            }
        }

        // ---------- API consommée par le manager ----------
        public void ShowLine(LineStep step)
        {
            if (choicesContainer) choicesContainer.SetActive(false);
            DeactivateAllChoices();

            if (line != null)
            {
                line.SetLineKey(step?.textKey ?? string.Empty);
                line.SetSpeakerKey(step?.speakerKey ?? string.Empty);
            }

            RequestAvatarSwap(step?.speakerKey, step?.expressionKey);
        }

        public void ShowChoices(ChoicesStep step)
        {
            if (line != null)
            {
                line.SetLineKey(string.Empty);
                line.SetSpeakerKey(string.Empty);
            }

            if (choicesContainer) choicesContainer.SetActive(true);
            DeactivateAllChoices();

            if (step == null || step.items == null) return;

            for (int i = 0; i < choiceSlots.Count; i++)
            {
                var slot = choiceSlots[i];
                if (!slot) continue;

                bool active = i < step.items.Count && step.items[i] != null;
                slot.gameObject.SetActive(active);
                if (!active) continue;

                var it = step.items[i];
                slot.SetMeta(it.optionId, it.index);
                slot.SetKey(it.textKey);
            }
        }

        public void HideAll()
        {
            if (choicesContainer) choicesContainer.SetActive(false);
            if (line != null)
            {
                line.SetLineKey(string.Empty);
                line.SetSpeakerKey(string.Empty);
            }
            DeactivateAllChoices();

            if (destroyAvatarOnHide)
            {
                ClearCurrentAvatar(true);
                ClearPreviousAvatar(true);
            }
        }

        // ---------- Avatars ----------
        private void RequestAvatarSwap(string speakerKey, string expressionKey)
        {
            if (_swapCo != null) StopCoroutine(_swapCo);
            _swapCo = StartCoroutine(SwapRoutine(speakerKey, expressionKey));
        }

        private IEnumerator SwapRoutine(string speakerKey, string expressionKey)
        {
            var curRoot = currentAvatarRoot ? currentAvatarRoot : avatarRoot;

            // Pas de speaker → clear courant, on garde le "previous" intact
            if (string.IsNullOrEmpty(speakerKey) || !_speakersById.TryGetValue(speakerKey, out var sp))
            {
                yield return ClearCurrentAvatar(waitTransitions);
                yield break;
            }

            var prefab = ResolvePrefab(sp, expressionKey);
            if (!prefab)
            {
                // pas de ressource pour cet acteur -> clear courant
                yield return ClearCurrentAvatar(waitTransitions);
                yield break;
            }

            // déjà le bon avatar + bonne expression → rien à faire
            if (_currentAvatar && _currentSpeakerKey == speakerKey && _currentExprKey == expressionKey)
                yield break;

            // 1) démote le courant vers previous
            if (_currentAvatar)
            {
                if (previousAvatarRoot)
                {
                    if (transition && waitTransitions)
                        yield return transition.DemoteToPrevious(_currentAvatar, previousAvatarRoot);

                    // Reparent instantané + fit
                    _currentAvatar.transform.SetParent(previousAvatarRoot, false);
                    FitToParent(_currentAvatar.transform);

                    // si un previous existait déjà, on le despawn
                    if (_previousAvatar && _previousAvatar != _currentAvatar)
                        yield return DespawnGo(_previousAvatar, waitTransitions);

                    _previousAvatar = _currentAvatar;
                    OnAvatarDemoted?.Invoke(_previousAvatar);
                }
                else
                {
                    // pas de slot previous → on le détruit
                    yield return DespawnGo(_currentAvatar, waitTransitions);
                }

                _currentAvatar = null;
            }

            // 2) spawn du nouveau courant
            if (curRoot)
            {
                _currentAvatar = Instantiate(prefab, curRoot);
                _currentAvatar.name = prefab.name + " (Current)";
                FitToParent(_currentAvatar.transform);

                if (transition && waitTransitions)
                    yield return transition.Spawn(_currentAvatar);

                OnAvatarSpawned?.Invoke(_currentAvatar);
            }

            _currentSpeakerKey = speakerKey;
            _currentExprKey = expressionKey;

            _swapCo = null;
        }

        private IEnumerator DespawnGo(GameObject go, bool wait)
        {
            if (!go) yield break;

            if (transition && wait)
                yield return transition.Despawn(go);

#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(go);
            else Destroy(go);
#else
            Destroy(go);
#endif
            OnAvatarDespawned?.Invoke(go);
        }

        private IEnumerator ClearCurrentAvatar(bool wait)
        {
            if (_currentAvatar == null) yield break;
            var toKill = _currentAvatar;
            _currentAvatar = null;
            _currentSpeakerKey = null; _currentExprKey = null;
            yield return DespawnGo(toKill, wait);
        }

        private IEnumerator ClearPreviousAvatar(bool wait)
        {
            if (_previousAvatar == null) yield break;
            var toKill = _previousAvatar;
            _previousAvatar = null;
            yield return DespawnGo(toKill, wait);
        }

        private static void FitToParent(Transform t)
        {
            if (!t) return;
            if (t is RectTransform rt)
            {
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.localScale = Vector3.one;
                rt.localRotation = Quaternion.identity;
            }
            else
            {
                t.localPosition = Vector3.zero;
                t.localScale = Vector3.one;
                t.localRotation = Quaternion.identity;
            }
        }

        private GameObject ResolvePrefab(Speaker s, string expressionKey)
        {
            if (!s) return null;

            if (!string.IsNullOrWhiteSpace(expressionKey))
            {
                var exp = s.Expressions?.FirstOrDefault(e => e != null && e.key == expressionKey);
                if (exp != null && exp.prefab) return exp.prefab;
            }

            var neutral = s.Expressions?.FirstOrDefault(e => e != null && e.key == "neutral");
            if (neutral != null && neutral.prefab) return neutral.prefab;

            var f = typeof(Speaker).GetField("defaultPrefab",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (f != null)
            {
                var go = f.GetValue(s) as GameObject;
                if (go) return go;
            }
            return null;
        }

        private void DeactivateAllChoices()
        {
            foreach (var c in choiceSlots)
                if (c && c.gameObject.activeSelf) c.gameObject.SetActive(false);
        }
    }
}
