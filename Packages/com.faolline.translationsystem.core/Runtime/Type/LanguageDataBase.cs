using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace com.faolline.translationsystem
{
    [CreateAssetMenu(fileName = "LanguageDatabase", menuName = "Translation/Language Database")]
    public class LanguageDataBase : ScriptableObject
    {
        [SerializeField]
        private List<SupportedLanguage> enabledLanguages = new();

        private void OnEnable()
        {
            if (enabledLanguages.Count == 0)
            {
                EnsureAtLeastOneLanguage();
            }
        }

        public IReadOnlyList<SupportedLanguage> EnabledLanguages => enabledLanguages;

        public bool IsLanguageEnabled(SupportedLanguage lang) => enabledLanguages.Contains(lang);

        public void SetLanguageEnabled(SupportedLanguage lang, bool enabled)
        {

            if (enabled && !enabledLanguages.Contains(lang))
                enabledLanguages.Add(lang);
            else if (!enabled && enabledLanguages.Contains(lang))
            {
                if (enabledLanguages.Count == 1)
                    Debug.LogWarning("Cannot disable the last enabled language. At least one language must be enabled.");
                else
                    enabledLanguages.Remove(lang);
            }
        }

        public SupportedLanguage? GetEnabledLanguage(string languageCode)
        {
            if (System.Enum.TryParse(languageCode, out SupportedLanguage lang) &&
                enabledLanguages.Contains(lang))
            {
                return lang;
            }
            else
            {
                Debug.LogWarning($"Language '{languageCode}' is not enabled or invalid.");
                return null;
            }
        }

        private void EnsureAtLeastOneLanguage()
        {
            // Supprime les doublons
            enabledLanguages = new HashSet<SupportedLanguage>(enabledLanguages).ToList();

            if (enabledLanguages.Count == 0)
            {
                enabledLanguages.Add(SupportedLanguage.EN);
                Debug.LogWarning("No language was enabled. Defaulted to EN.");
            }
        }
    }
}


