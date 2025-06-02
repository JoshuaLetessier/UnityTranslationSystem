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

            isLoaded = true;
        }

        public static void ReloadAll(IEnumerable<SupportedLanguage> enabledLanguages)
        {
            isLoaded = false;
            LoadAll(enabledLanguages);
        }

        public static string Get(SupportedLanguage lang, string key)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Debug.LogWarning("TranslationService.Get() called outside of PlayMode. Fallback to key.");
                return key;
            }
#endif
            var manager = LanguageManager.Instance;
            if (manager == null || manager.GetLanguageDataBase() == null)
            {
                //Debug.LogWarning("TranslationService: LanguageManager or LanguageDataBase not available.");
                return key;
            }

            LoadAll(manager.GetLanguageDataBase().EnabledLanguages);

            if (translationsByLang.TryGetValue(lang, out var dict) && dict.TryGetValue(key, out var value))
                return value;

            Debug.LogWarning($"Missing translation: [{lang}] {key}");
            return key;
        }
    }
}
