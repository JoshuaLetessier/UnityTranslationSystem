using System.Linq;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem; // Keyboard
#endif

namespace com.faolline.dialoguesystem
{
    /// <summary>
    /// Contrôle utilisateur minimal (sans UI métier) :
    ///  - [Space] : runner.Proceed() quand une Line est prête
    ///  - [1..9] / [Numpad 1..9] : runner.ChooseByIndex() quand des choix sont prêts
    /// Affiche via DialogueUIAdapter si présent.
    /// </summary>
    public class DialogueManagerUserControlled : MonoBehaviour
    {
        [Header("Setup")]
        [SerializeField] private Dialogue dialogue;
        [SerializeField] private DialogueUIAdapter ui;
        [SerializeField] private bool autoStart = true;

        [Header("Debug Overlay")]
        [SerializeField] private bool showOverlay = true;
        [SerializeField] private Vector2 overlayPos = new Vector2(10, 10);

        private DialogueRunner _runner;
        private ChoicesStep _lastChoices;
        private LineStep _lastLine;
        private EndStep _lastEnd;

        private void Start()
        {
            if (autoStart && dialogue != null)
                StartDialogue(dialogue);
        }

        public void StartDialogue(Dialogue d)
        {
            if (d == null)
            {
                Debug.LogError("[DialogueManagerUserControlled] Dialogue manquant.");
                return;
            }

            UnsubscribeRunnerEvents();
            _runner = new DialogueRunner(d);

            _runner.OnLine += HandleLine;
            _runner.OnChoices += HandleChoices;
            _runner.OnEnd += HandleEnd;

            _runner.Start(); // émet la première Line (Start) via OnLine
        }

        private void Update()
        {
            if (_runner == null || _runner.State == RunnerState.Ended) return;

            // Avance quand une Line est prête
            if (_runner.State == RunnerState.LineReady && WasSpacePressedThisFrame())
            {
                _runner.Proceed();
                return;
            }

            // Choix quand des options sont prêtes
            if (_runner.State == RunnerState.ChoiceReady && _lastChoices != null)
            {
                var idx = GetPressedChoiceIndexThisFrame();
                if (idx > 0)
                {
                    if (_lastChoices.items.Any(i => i.index == idx && i.allowed))
                        _runner.ChooseByIndex(idx);
                    else
                        Debug.Log($"[Dialogue] Choix {idx} indisponible (bloqué ou inexistant).");
                }
            }
        }

        private void OnDestroy() => UnsubscribeRunnerEvents();

        private void UnsubscribeRunnerEvents()
        {
            if (_runner == null) return;
            _runner.OnLine -= HandleLine;
            _runner.OnChoices -= HandleChoices;
            _runner.OnEnd -= HandleEnd;
        }

        // ---- Handlers (branchent l’UI adapter) ----
        private void HandleLine(LineStep s)
        {
            _lastLine = s; _lastChoices = null; _lastEnd = null;
            ui?.ShowLine(s);
        }

        private void HandleChoices(ChoicesStep s)
        {
            _lastChoices = s; _lastLine = null; _lastEnd = null;
            ui?.ShowChoices(s);

            var desc = string.Join(", ", s.items.Select(i =>
                $"{i.index}:{i.textKey} ({i.optionId}){(i.allowed ? "" : " [blocked]")}"));
        }

        private void HandleEnd(EndStep s)
        {
            _lastEnd = s; _lastChoices = null; _lastLine = null;
            ui?.HideAll();
        }

        // ---- Input helpers ----
        private bool WasSpacePressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            return kb != null && kb.spaceKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Space);
#endif
        }

        private int GetPressedChoiceIndexThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null) return 0;

            if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) return 1;
            if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) return 2;
            if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) return 3;
            if (kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame) return 4;
            if (kb.digit5Key.wasPressedThisFrame || kb.numpad5Key.wasPressedThisFrame) return 5;
            if (kb.digit6Key.wasPressedThisFrame || kb.numpad6Key.wasPressedThisFrame) return 6;
            if (kb.digit7Key.wasPressedThisFrame || kb.numpad7Key.wasPressedThisFrame) return 7;
            if (kb.digit8Key.wasPressedThisFrame || kb.numpad8Key.wasPressedThisFrame) return 8;
            if (kb.digit9Key.wasPressedThisFrame || kb.numpad9Key.wasPressedThisFrame) return 9;
            return 0;
#else
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) return 1;
            if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) return 2;
            if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) return 3;
            if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) return 4;
            if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5)) return 5;
            if (Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.Keypad6)) return 6;
            if (Input.GetKeyDown(KeyCode.Alpha7) || Input.GetKeyDown(KeyCode.Keypad7)) return 7;
            if (Input.GetKeyDown(KeyCode.Alpha8) || Input.GetKeyDown(KeyCode.Keypad8)) return 8;
            if (Input.GetKeyDown(KeyCode.Alpha9) || Input.GetKeyDown(KeyCode.Keypad9)) return 9;
            return 0;
#endif
        }

        // ---- Overlay debug (optionnel) ----
        private void OnGUI()
        {
            if (!showOverlay || _runner == null) return;

            var styleTitle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            var styleMono = new GUIStyle(GUI.skin.label) { fontSize = 12 };

            float x = overlayPos.x, y = overlayPos.y, w = 680f, h = 20f;

            GUI.Label(new Rect(x, y, w, h), "Dialogue (user-controlled)", styleTitle); y += h;
            GUI.Label(new Rect(x, y, w, h), $"State: {_runner.State}  |  Node: {_runner.CurrentNodeId}", styleMono); y += h;

            if (_lastLine != null)
            {
                GUI.Label(new Rect(x, y, w, h),
                    $"LINE — key='{_lastLine.textKey}' (speaker='{_lastLine.speakerId}', mood={_lastLine.speakerMood})",
                    styleMono); y += h;
                GUI.Label(new Rect(x, y, w, h), "Press [SPACE] to proceed", styleMono); y += h;
            }
            else if (_lastChoices != null)
            {
                GUI.Label(new Rect(x, y, w, h), "CHOICES — select with [1..9] or [Numpad 1..9]:", styleMono); y += h;
                foreach (var it in _lastChoices.items)
                {
                    var tag = it.allowed ? "" : " [blocked]";
                    GUI.Label(new Rect(x + 10, y, w, h), $"{it.index}) key='{it.textKey}'  ({it.optionId}){tag}", styleMono); y += h;
                }
            }
            else if (_lastEnd != null)
            {
                GUI.Label(new Rect(x, y, w, h), $"END — reason={_lastEnd.reason}, error='{_lastEnd.error}'", styleMono); y += h;
            }
        }
    }
}
