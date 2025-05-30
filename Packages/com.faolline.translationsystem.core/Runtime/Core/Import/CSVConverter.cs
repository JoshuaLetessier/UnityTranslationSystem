using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace com.faolline.translationsystem
{
    public static class CSVConverter
    {
        [MenuItem("Tools/Translation/Generate JSON From CSV")]
        public static void GenerateJsonFile()
        {
            string basePath = Path.Combine(Application.dataPath, "Translations");
            string folderPath = Path.Combine(basePath, "CSV");
            string outputFolder = Path.Combine(basePath, "Generated");

            if (!Directory.Exists(folderPath))
            {
                Debug.LogError("📁 Folder not found: " + folderPath);
                return;
            }

            if (!Directory.Exists(outputFolder))
                Directory.CreateDirectory(outputFolder);

            var langMap = new Dictionary<string, Dictionary<string, string>>();
            var allKeys = new HashSet<string>();

            foreach (string filePath in Directory.GetFiles(folderPath, "*.csv", SearchOption.AllDirectories))
            {
                string[] lines = File.ReadAllLines(filePath);
                if (lines.Length < 2) continue;

                string[] headers = lines[0].Split(',');

                for (int i = 1; i < headers.Length; i++)
                {
                    string lang = headers[i].Trim().ToUpper();
                    if (!langMap.ContainsKey(lang))
                        langMap[lang] = new Dictionary<string, string>();
                }

                for (int i = 1; i < lines.Length; i++)
                {
                    string[] parts = lines[i].Split(',');
                    if (parts.Length < 2) continue;

                    string key = parts[0].Trim();
                    if (string.IsNullOrEmpty(key)) continue;

                    allKeys.Add(key);

                    for (int j = 1; j < parts.Length; j++)
                    {
                        string lang = headers[j].Trim().ToUpper();
                        string value = j < parts.Length ? parts[j].Trim() : "";

                        if (!string.IsNullOrEmpty(value))
                        {
                            langMap[lang][key] = value;
                        }
                    }
                }
            }

            // Générer JSON par langue
            foreach (var lang in langMap.Keys)
            {
                string path = Path.Combine(outputFolder, lang.ToLower() + ".json");
                File.WriteAllText(path, JsonConvert.SerializeObject(langMap[lang], Formatting.Indented));
                Debug.Log($"✅ JSON generated: {path}");
            }

            // Générer TranslationKeyDatabase groupé
            var grouped = new Dictionary<string, List<string>>();
            foreach (var key in allKeys)
            {
                var parts = key.Split('.');
                if (parts.Length < 2) continue;

                string category = parts[0];
                if (!grouped.ContainsKey(category))
                    grouped[category] = new List<string>();

                grouped[category].Add(key);
            }

            var database = ScriptableObject.CreateInstance<TranslationKeyDatabase>();
            var keyGroups = new List<TranslationKeyGroup>();

            foreach (var pair in grouped)
            {
                keyGroups.Add(new TranslationKeyGroup
                {
                    category = pair.Key,
                    keys = pair.Value
                });
            }

            database.SetKeyGroups(keyGroups);

            string assetPath = "Assets/Translations/Generated/TranslationKeyDatabase.asset";
            AssetDatabase.CreateAsset(database, assetPath);
            AssetDatabase.SaveAssets();

            Debug.Log("✅ TranslationKeyDatabase.asset generated.");
            AssetDatabase.Refresh();
        }
    }
}
