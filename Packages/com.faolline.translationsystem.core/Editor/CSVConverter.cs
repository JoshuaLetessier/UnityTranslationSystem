using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Text;
using System;
using System.Linq;

namespace com.faolline.translationsystem
{
    public static class CSVConverter
    {
        private const string MenuGen = "Tools/Translation/Generate JSON + Key DB (Resources)";
        private const string MenuGenKeysOnly = "Tools/Translation/Rebuild Key DB Only";

        [MenuItem(MenuGen)]
        public static void GenerateJsonFilesAndKeyDB()
        {
            try
            {
                PrepareFolders(out var csvFolder, out var outputFolder, out var keyDbAssetPath);

                var csvFiles = Directory.GetFiles(csvFolder, "*.csv", SearchOption.AllDirectories);
                if (csvFiles.Length == 0)
                {
                    Debug.LogWarning($"[Translation] Aucun CSV trouvé dans: {Rel(csvFolder)}");
                    EditorUtility.DisplayDialog("Translation", "Aucun CSV trouvé dans Resources/Translations/CSV.", "OK");
                    AssetDatabase.Refresh();
                    return;
                }

                // lang -> (key -> value)
                var perLanguage = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
                var allKeys = new HashSet<string>(StringComparer.Ordinal);
                int totalRows = 0, totalKeys = 0;

                foreach (var csvFile in csvFiles)
                    ParseCsvFileIntoDictionaries(csvFile, perLanguage, allKeys, ref totalRows, ref totalKeys);

                // Write JSON per language
                int filesWritten = 0;
                foreach (var (lang, dict) in perLanguage)
                {
                    var json = JsonConvert.SerializeObject(dict, Formatting.Indented);
                    var outPath = Path.Combine(outputFolder, $"{lang.ToLowerInvariant()}.json");
                    File.WriteAllText(outPath, json, new UTF8Encoding(false));
                    filesWritten++;
                    Debug.Log($"[Translation] Écrit: {Rel(outPath)} ({dict.Count} entrées)");
                }

                // Build / Update TranslationKeyDatabase.asset
                BuildOrUpdateKeyDatabaseAsset(keyDbAssetPath, allKeys);

                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog(
                    "Translation",
                    $"Génération terminée.\n\nCSV lus : {csvFiles.Length}\nLignes traitées : {totalRows}\nEntrées écrites : {totalKeys}\nFichiers JSON : {filesWritten}\nKeys uniques : {allKeys.Count}\n\nSortie JSON : Assets/Resources/Translations/Generated\nKey DB : {keyDbAssetPath}",
                    "OK"
                );
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Translation] Erreur: {ex}");
                EditorUtility.DisplayDialog("Translation - Erreur", ex.Message, "OK");
            }
        }

        [MenuItem(MenuGenKeysOnly)]
        public static void RebuildKeyDatabaseOnly()
        {
            try
            {
                PrepareFolders(out var csvFolder, out _, out var keyDbAssetPath);

                var csvFiles = Directory.GetFiles(csvFolder, "*.csv", SearchOption.AllDirectories);
                if (csvFiles.Length == 0)
                {
                    Debug.LogWarning($"[Translation] Aucun CSV trouvé dans: {Rel(csvFolder)}");
                    EditorUtility.DisplayDialog("Translation", "Aucun CSV trouvé dans Resources/Translations/CSV.", "OK");
                    return;
                }

                var allKeys = new HashSet<string>(StringComparer.Ordinal);
                int dummyRows = 0, dummyKeys = 0;
                foreach (var csvFile in csvFiles)
                {
                    // parsage minimal pour extraire les keys uniquement
                    ParseCsvFileIntoDictionaries(csvFile,
                        perLanguage: null,
                        allKeys: allKeys,
                        totalRows: ref dummyRows,
                        totalKeys: ref dummyKeys,
                        valuesOptional: true);
                }

                BuildOrUpdateKeyDatabaseAsset(keyDbAssetPath, allKeys);
                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog(
                    "Translation",
                    $"Key DB reconstruite.\nKeys uniques : {allKeys.Count}\nAsset : {keyDbAssetPath}",
                    "OK"
                );
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Translation] Erreur: {ex}");
                EditorUtility.DisplayDialog("Translation - Erreur", ex.Message, "OK");
            }
        }

        // ---------- Core ----------

        private static void ParseCsvFileIntoDictionaries(
            string csvFile,
            Dictionary<string, Dictionary<string, string>> perLanguage,
            HashSet<string> allKeys,
            ref int totalRows,
            ref int totalKeys,
            bool valuesOptional = false)
        {
            var text = File.ReadAllText(csvFile, new UTF8Encoding(false));
            if (string.IsNullOrWhiteSpace(text))
            {
                Debug.LogWarning($"[Translation] CSV vide: {Rel(csvFile)}");
                return;
            }

            var rows = ParseCsv(text, DetectSeparator(text));
            if (rows.Count == 0) return;

            var header = rows[0];
            if (header.Length < 1)
            {
                Debug.LogWarning($"[Translation] En-tête invalide dans: {Rel(csvFile)}");
                return;
            }

            int keyCol = Array.FindIndex(header, h => h.Trim().Equals("key", StringComparison.OrdinalIgnoreCase));
            if (keyCol < 0)
            {
                Debug.LogWarning($"[Translation] Colonne 'key' introuvable dans: {Rel(csvFile)}");
                return;
            }

            var langCols = new List<(string lang, int idx)>();
            if (!valuesOptional)
            {
                for (int i = 0; i < header.Length; i++)
                {
                    if (i == keyCol) continue;
                    var lang = header[i].Trim();
                    if (string.IsNullOrEmpty(lang)) continue;
                    langCols.Add((lang, i));
                    perLanguage ??= new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
                    if (!perLanguage.ContainsKey(lang))
                        perLanguage[lang] = new Dictionary<string, string>(StringComparer.Ordinal);
                }
            }

            for (int r = 1; r < rows.Count; r++)
            {
                var row = rows[r];
                totalRows++;
                if (row.Length <= keyCol) continue;

                var key = row[keyCol].Trim();
                if (string.IsNullOrEmpty(key)) continue;

                allKeys?.Add(key);

                if (valuesOptional) continue;

                foreach (var (lang, idx) in langCols)
                {
                    if (idx >= row.Length) continue;
                    var value = row[idx].Trim();
                    if (string.IsNullOrEmpty(value)) continue;

                    var dict = perLanguage[lang];
                    if (dict.ContainsKey(key))
                        Debug.LogWarning($"[Translation] Clé dupliquée '{key}' pour '{lang}' (overwrite) dans {Rel(csvFile)}");
                    else
                        totalKeys++;

                    dict[key] = value;
                }
            }
        }

        private static void BuildOrUpdateKeyDatabaseAsset(string keyDbAssetPath, HashSet<string> allKeys)
        {
            // Tri & unicité
            var sorted = allKeys?.ToList() ?? new List<string>();
            sorted.Sort(StringComparer.Ordinal);

            // Créer ou charger l’asset
            var keyDb = AssetDatabase.LoadAssetAtPath<TranslationKeyDatabase>(keyDbAssetPath);
            if (keyDb == null)
            {
                keyDb = ScriptableObject.CreateInstance<TranslationKeyDatabase>();
                keyDb.SetKeys(sorted);
                EnsureDirectory(Path.GetDirectoryName(keyDbAssetPath)!);
                AssetDatabase.CreateAsset(keyDb, keyDbAssetPath);
                Debug.Log($"[Translation] Key DB créé: {keyDbAssetPath} ({sorted.Count} keys)");
            }
            else
            {
                keyDb.SetKeys(sorted);
                EditorUtility.SetDirty(keyDb);
                Debug.Log($"[Translation] Key DB mise à jour: {keyDbAssetPath} ({sorted.Count} keys)");
            }

            AssetDatabase.SaveAssets();
        }

        private static void PrepareFolders(out string csvFolder, out string outputFolder, out string keyDbAssetPath)
        {
            var assetsPath = Application.dataPath; // <project>/Assets
            var resourcesPath = Path.Combine(assetsPath, "Resources");
            var translationsPath = Path.Combine(resourcesPath, "Translations");

            csvFolder = Path.Combine(translationsPath, "CSV");
            outputFolder = Path.Combine(translationsPath, "Generated");
            keyDbAssetPath = "Assets/Resources/Translations/TranslationKeyDatabase.asset";

            EnsureDirectory(resourcesPath);
            EnsureDirectory(translationsPath);
            EnsureDirectory(csvFolder);
            EnsureDirectory(outputFolder);
        }

        // ---------- Utils ----------

        private static void EnsureDirectory(string absolutePath)
        {
            if (!Directory.Exists(absolutePath))
            {
                Directory.CreateDirectory(absolutePath);
                Debug.Log($"[Translation] Dossier créé: {Rel(absolutePath)}");
            }
        }

        private static string Rel(string absolutePath)
        {
            var projectRoot = Path.GetDirectoryName(Application.dataPath)?.Replace('\\', '/');
            return absolutePath.Replace('\\', '/').Replace(projectRoot + "/", "");
        }

        private static char DetectSeparator(string csvText)
        {
            var firstLine = csvText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).FirstOrDefault() ?? "";
            int commas = firstLine.Count(c => c == ',');
            int semis = firstLine.Count(c => c == ';');
            return semis > commas ? ';' : ',';
        }

        // Basic RFC4180 parser (quotes, escaped quotes, separators, newlines inside quotes)
        private static List<string[]> ParseCsv(string text, char sep)
        {
            var rows = new List<string[]>();
            var field = new StringBuilder();
            var row = new List<string>();
            bool inQuotes = false;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (inQuotes)
                {
                    if (c == '\"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '\"')
                        {
                            field.Append('\"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else field.Append(c);
                }
                else
                {
                    if (c == '\"') inQuotes = true;
                    else if (c == sep) { row.Add(field.ToString()); field.Length = 0; }
                    else if (c == '\r') { /* ignore */ }
                    else if (c == '\n') { row.Add(field.ToString()); field.Length = 0; rows.Add(row.ToArray()); row.Clear(); }
                    else field.Append(c);
                }
            }

            row.Add(field.ToString());
            rows.Add(row.ToArray());
            return rows;
        }
    }
}