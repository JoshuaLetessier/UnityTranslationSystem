using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace com.faolline.translationsystem
{
    public class CSVManager
    {
        public void export(ref List<SheetTemplate> sheets, ref List<SupportedLanguage> languages)
        {
            // Dossier cible UPM/Runtime-friendly
            string assetsFolder = "Assets/Resources/Translations/CSV";
            string folderPathFs = Path.Combine(Application.dataPath, "Resources/Translations/CSV");

            if (!Directory.Exists(folderPathFs))
                Directory.CreateDirectory(folderPathFs);

            foreach (var sheet in sheets)
            {
                // nom de fichier safe
                string safeName = string.Join("_", (sheet.Name ?? "Sheet").Split(Path.GetInvalidFileNameChars()));
                var sb = new StringBuilder();

                // Header
                sb.Append("Key");
                foreach (var lang in languages)
                    sb.Append($",{lang.ToString().ToUpper()}");
                sb.AppendLine();

                // Lignes
                foreach (var row in sheet.Rows)
                {
                    sb.Append(EscapeCsv(row.key ?? string.Empty));
                    for (int i = 0; i < languages.Count; i++)
                    {
                        string val = (i < row.cells.Count) ? (row.cells[i].text ?? string.Empty) : string.Empty;
                        sb.Append($",{EscapeCsv(val)}");
                    }
                    sb.AppendLine();
                }

                // Écriture FS + import asset
                string fileFs = Path.Combine(folderPathFs, safeName + ".csv");
                File.WriteAllText(fileFs, sb.ToString(), Encoding.UTF8);

                string assetPath = $"{assetsFolder}/{safeName}.csv";
                AssetDatabase.ImportAsset(assetPath); // GUID conservé si l’asset existait
                Debug.Log($"✅ CSV exported: {assetPath}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // --- local helpers ---
            static string EscapeCsv(string s)
            {
                // double quotes + wrap if needed
                bool needQuotes = s.Contains(",") || s.Contains("\"") || s.Contains("\n") || s.Contains("\r");
                s = s.Replace("\"", "\"\"");
                return needQuotes ? $"\"{s}\"" : s;
            }
        }


        public void import(ref List<SheetTemplate> sheets)
        {
            sheets.Clear();

            // Nouveau chemin + découverte via AssetDatabase
            const string assetsFolder = "Assets/Resources/Translations/CSV";
            if (!AssetDatabase.IsValidFolder(assetsFolder))
                return;

            var guids = AssetDatabase.FindAssets($"t:{nameof(TextAsset)}", new[] { assetsFolder });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var ta = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                if (ta == null) continue;

                var lines = SplitCsvLines(ta.text);
                if (lines.Count < 2) continue;

                var headers = ParseCsvLine(lines[0]);
                if (headers.Count < 2 || headers[0] != "Key") continue;

                var sheet = new SheetTemplate
                {
                    Name = ta.name,
                    Rows = new List<RowTemplate>()
                };

                for (int i = 1; i < lines.Count; i++)
                {
                    var cols = ParseCsvLine(lines[i]);
                    if (cols.Count == 0) continue;

                    var row = new RowTemplate { key = cols[0], cells = new List<CellTemplate>() };
                    for (int j = 1; j < headers.Count; j++)
                    {
                        string val = (j < cols.Count) ? cols[j] : string.Empty;
                        row.cells.Add(new CellTemplate { text = val });
                    }
                    sheet.Rows.Add(row);
                }

                sheets.Add(sheet);
            }

            // --- local helpers robustes ---
            static List<string> SplitCsvLines(string text)
            {
                var list = new List<string>();
                using (var sr = new StringReader(text))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                        list.Add(line);
                }
                return list;
            }

            static List<string> ParseCsvLine(string line)
            {
                var res = new List<string>();
                if (line == null) { res.Add(string.Empty); return res; }

                var sb = new StringBuilder();
                bool inQuotes = false;

                for (int i = 0; i < line.Length; i++)
                {
                    char c = line[i];
                    if (inQuotes)
                    {
                        if (c == '"')
                        {
                            bool hasNext = (i + 1 < line.Length);
                            if (hasNext && line[i + 1] == '"') { sb.Append('"'); i++; } // escaped quote
                            else inQuotes = false;
                        }
                        else sb.Append(c);
                    }
                    else
                    {
                        if (c == ',') { res.Add(sb.ToString()); sb.Length = 0; }
                        else if (c == '"') inQuotes = true;
                        else sb.Append(c);
                    }
                }
                res.Add(sb.ToString());
                return res;
            }
        }

    }
}
