using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using System.Linq;

namespace com.faolline.translationsystem
{
    public static class TranslationLoader
    {
        private static readonly Dictionary<SupportedLanguage, Dictionary<string, string>> translationsByLang =
            new Dictionary<SupportedLanguage, Dictionary<string, string>>();

        public static void LoadAll(List<SupportedLanguage> enabledLanguages)
        {
            translationsByLang.Clear();
            string folderPath = Path.Combine(Application.dataPath, "Translations/Generated");

            foreach (var lang in enabledLanguages)
            {
                string filePath = Path.Combine(folderPath, lang.ToString().ToLower() + ".json");
                if (!File.Exists(filePath))
                {
                    Debug.LogWarning($"Translation file not found: {filePath}");
                    continue;
                }

                try
                {
                    string json = File.ReadAllText(filePath);
                    var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                    translationsByLang[lang] = dict;
                }
                catch
                {
                    Debug.LogError($"Failed to parse translation file: {filePath}");
                }
            }

        }

        public static void ReloadAll(List<SupportedLanguage> enabledLanguages)
        {
            translationsByLang.Clear();
            LoadAll(enabledLanguages);
        }

        public static string GetTranslation(SupportedLanguage lang, string key)
        {
            // Fix: Use LanguageManager.Instance to access the singleton instance
            LoadAll(LanguageManager.Instance.GetLanguageDataBase().EnabledLanguages.ToList());

            if (translationsByLang.TryGetValue(lang, out var dict) && dict.TryGetValue(key, out var value))
                return value;

            Debug.LogWarning($"Missing translation: [{lang}] {key}");
            return key; // fallback to key
        }
    }
}
