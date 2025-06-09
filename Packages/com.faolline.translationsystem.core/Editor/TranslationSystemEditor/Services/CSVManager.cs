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
            string folderPath = Path.Combine(Application.dataPath, "Translations/CSV");
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            foreach (var sheet in sheets)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("Key");
                foreach (var lang in languages)
                {
                    sb.Append($",{lang.ToString().ToUpper()}");
                }
                sb.AppendLine();

                foreach (var row in sheet.Rows)
                {
                    sb.Append(row.key);
                    for (int i = 0; i < languages.Count; i++)
                    {
                        string val = (i < row.cells.Count) ? row.cells[i].text : "";
                        val = val.Replace("\"", "\"\""); // escape quotes
                        sb.Append($",\"{val}\"");
                    }
                    sb.AppendLine();
                }

                string filePath = Path.Combine(folderPath, sheet.Name + ".csv");
                File.WriteAllText(filePath, sb.ToString());
                Debug.Log($"✅ CSV exported: {filePath}");
            }

            AssetDatabase.Refresh();
        }

        public void import(ref List<SheetTemplate> sheets)
        {
            sheets.Clear();
            string folderPath = Path.Combine(Application.dataPath, "Translations/CSV");
            if (!Directory.Exists(folderPath)) return;

            foreach (var file in Directory.GetFiles(folderPath, "*.csv"))
            {
                var lines = File.ReadAllLines(file);
                if (lines.Length < 2) continue;

                var headers = lines[0].Split(',');
                if (headers.Length < 2 || headers[0] != "Key") continue;

                var sheet = new SheetTemplate
                {
                    Name = Path.GetFileNameWithoutExtension(file),
                    Rows = new List<RowTemplate>()
                };

                for (int i = 1; i < lines.Length; i++)
                {
                    var parts = lines[i].Split(',');
                    if (parts.Length < 1) continue;

                    var row = new RowTemplate { key = parts[0] };
                    for (int j = 1; j < headers.Length; j++)
                    {
                        if (j < parts.Length)
                        {
                            string val = parts[j].Trim().Trim('"').Replace("\"\"", "\"");
                            row.cells.Add(new CellTemplate { text = val });
                        }
                        else
                        {
                            row.cells.Add(new CellTemplate());
                        }
                    }
                    sheet.Rows.Add(row);
                }
                sheets.Add(sheet);
            }
        }
    }
}
