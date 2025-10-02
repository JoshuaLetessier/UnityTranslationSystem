using System;
using System.Collections.Generic;
using UnityEngine;

namespace com.faolline.dialoguesystem
{
    [Serializable]
    public class SpeakerExpression
    {
        [Tooltip("Clé logique (ex: neutral, happy, angry...)")]
        public string key = "neutral";

        [Tooltip("Prefab à instancier pour cette expression (sprite, animator, vfx, UI...)")]
        public GameObject prefab;
    }

    public enum NameDisplayMode { Literal, TranslationKey }

    [CreateAssetMenu(menuName = "Dialogue System/Speaker", fileName = "NewSpeaker")]
    public class Speaker : ScriptableObject
    {
        // Identifiant logique utilisé par le graphe (ne pas traduire)
        [SerializeField] private string speakerName = "New Speaker";
        public string SpeakerName { get => speakerName; set => speakerName = value; }

        // Affichage du nom (localisable optionnel)
        [SerializeField] private NameDisplayMode displayNameMode = NameDisplayMode.Literal;
        [SerializeField] private string displayNameLiteral = "New Speaker";
        [SerializeField] private string displayNameKey = "";

        public NameDisplayMode DisplayNameMode { get => displayNameMode; set => displayNameMode = value; }
        public string DisplayNameLiteral { get => displayNameLiteral; set => displayNameLiteral = value; }
        public string DisplayNameKey { get => displayNameKey; set => displayNameKey = value; }

        [Header("Expressions (clé → prefab)")]
        [SerializeField] private List<SpeakerExpression> expressions = new();
        public IReadOnlyList<SpeakerExpression> Expressions => expressions;

        [Header("Fallback")]
        [SerializeField] private GameObject defaultExpressionPrefab; // utilisé si la clé n’existe pas

        public bool TryGetExpressionPrefab(string expressionKey, out GameObject prefab)
        {
            if (!string.IsNullOrEmpty(expressionKey))
            {
                var exp = expressions.Find(e => e != null && e.key == expressionKey);
                if (exp != null && exp.prefab != null)
                {
                    prefab = exp.prefab;
                    return true;
                }
            }

            if (defaultExpressionPrefab != null)
            {
                prefab = defaultExpressionPrefab;
                return true;
            }

            prefab = null;
            return false;
        }

        public GameObject GetExpressionPrefabOrNull(string expressionKey)
        {
            TryGetExpressionPrefab(expressionKey, out var go);
            return go;
        }

        public IEnumerable<string> GetExpressionKeys()
        {
            foreach (var e in expressions)
                if (e != null && !string.IsNullOrEmpty(e.key))
                    yield return e.key;
        }

#if COM_FAOLLINE_TRANSLATIONSYSTEM
        public string GetDisplayName(SupportedLanguage lang)
        {
            if (displayNameMode == NameDisplayMode.TranslationKey && !string.IsNullOrEmpty(displayNameKey))
            {
                var v = TranslationService.Get(lang, displayNameKey);
                if (!string.IsNullOrEmpty(v)) return v;
            }
            return string.IsNullOrEmpty(displayNameLiteral) ? speakerName : displayNameLiteral;
        }

        public string GetDisplayNameCurrent()
        {
            var lm = LanguageManager.Instance;
            var lang = lm != null ? lm.GetCurrentLanguage() : SupportedLanguage.EN;
            return GetDisplayName(lang);
        }

        public string GetExpressionDisplay(string expressionKey, SupportedLanguage lang)
        {
            if (string.IsNullOrEmpty(expressionKey)) return expressionKey ?? "";
            var exp = expressions.Find(e => e != null && e.key == expressionKey);
            if (exp != null && !string.IsNullOrEmpty(exp.displayKey))
            {
                var v = TranslationService.Get(lang, exp.displayKey);
                if (!string.IsNullOrEmpty(v)) return v;
            }
            return expressionKey; // fallback : montre la clé
        }

        public string GetExpressionDisplayCurrent(string expressionKey)
        {
            var lm = LanguageManager.Instance;
            var lang = lm != null ? lm.GetCurrentLanguage() : SupportedLanguage.EN;
            return GetExpressionDisplay(expressionKey, lang);
        }
#else
        public string GetDisplayName(object _ = null)
            => string.IsNullOrEmpty(displayNameLiteral) ? speakerName : displayNameLiteral;
        public string GetDisplayNameCurrent() => GetDisplayName(null);

        public string GetExpressionDisplay(string expressionKey, object _ = null) => expressionKey ?? "";
        public string GetExpressionDisplayCurrent(string expressionKey) => GetExpressionDisplay(expressionKey, null);
#endif
    }
}
