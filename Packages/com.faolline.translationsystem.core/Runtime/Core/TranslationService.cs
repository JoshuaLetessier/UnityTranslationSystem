using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;

namespace com.faolline.translationsystem
{
    public static class TranslationService
    {
        private static readonly Dictionary<SupportedLanguage, Dictionary<string, string>> translationsByLang = new();
        private static bool isLoaded = false;

        public static void LoadAll(IEnumerable<SupportedLanguage> enabledLanguages)
        {
            if (isLoaded) return;

            translationsByLang.Clear();

            foreach (var lang in enabledLanguages)
            {
                // Path Resources sans extension
                var resourcePath = $"Translations/Generated/{lang.ToString().ToLower()}";
                var textAsset = Resources.Load<TextAsset>(resourcePath);

                if (textAsset == null)
                {
                    Debug.LogWarning($"Translation file not found in Resources: {resourcePath}.json");
                    continue;
                }

                try
                {
                    var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(textAsset.text);
                    translationsByLang[lang] = dict ?? new Dictionary<string, string>();
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Failed to parse translations for {lang}: {ex.Message}");
                }
            }

            isLoaded = true;
        }

        public static void ReloadAll(IEnumerable<SupportedLanguage> enabledLanguages)
        {
            isLoaded = false;
            LoadAll(enabledLanguages);
        }

        public static string Get(SupportedLanguage lang, string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;

            var manager = LanguageManager.Instance;
            if (manager == null)
                return key;

            LoadAll(manager.GetLanguageDataBase().EnabledLanguages);

            if (translationsByLang.TryGetValue(lang, out var dict) && dict != null && dict.TryGetValue(key, out var value))
                return value;

            Debug.LogWarning($"Missing translation: [{lang}] {key}");
            return key;
        }
    }
}
