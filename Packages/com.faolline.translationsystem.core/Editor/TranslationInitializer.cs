using UnityEditor;
using UnityEngine;
using System.IO;

namespace com.faolline.translationsystem
{
    [InitializeOnLoad]
    public static class TranslationInitializer
    {
        static TranslationInitializer()
        {
            string translationsPath = Path.Combine("Assets", "Translations");
            string csvPath = Path.Combine(translationsPath, "CSV");
            string genPath = Path.Combine(translationsPath, "Generated");
            string languageAssetPath = Path.Combine(translationsPath, "LanguageDatabase.asset");

            bool created = false;

            if (!Directory.Exists(translationsPath))
            {
                Directory.CreateDirectory(translationsPath);
                created = true;
            }

            if (!Directory.Exists(csvPath))
            {
                Directory.CreateDirectory(csvPath);
                created = true;
            }

            if (!Directory.Exists(genPath))
            {
                Directory.CreateDirectory(genPath);
                created = true;
            }

            if (!File.Exists(languageAssetPath))
            {
                var db = ScriptableObject.CreateInstance<LanguageDataBase>();
                db.SetLanguages(new List<SupportedLanguage> { SupportedLanguage.EN });
                AssetDatabase.CreateAsset(db, languageAssetPath);
                AssetDatabase.SaveAssets();
                Debug.Log("📝 LanguageDatabase.asset created in Translations/");
            }

            if (created)
            {
                Debug.Log("📁 Translation folders created in Assets/Translations/");
                AssetDatabase.Refresh();
            }
        }
    }
}
