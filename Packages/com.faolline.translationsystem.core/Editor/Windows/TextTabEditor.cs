using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

namespace com.faolline.translationsystem
{
    public class TextTabEditor : EditorWindow
    {
        private List<SheetTemplate> sheets = new();
        private Vector2 scrollPos;
        private Vector2 bottomScrollPos;
        private int selectedSheetIndex = -1;
        private List<SupportedLanguage> languages = new();

        private int selectedRow = -1;
        private int selectedColumn = -1;

        private CSVManager csvManager = new CSVManager();



        [MenuItem("Window/Translation System/Text Tab Editor")]
        public static void ShowWindow()
        {
            GetWindow<TextTabEditor>("Text Tab Editor");
        }

        private void OnEnable()
        {
            LoadLanguages();
            csvManager.import(ref sheets);
            if (sheets.Count == 0)
                AddSheet("Default");
        }

        private void LoadLanguages()
        {
            var manager = LanguageManager.Instance;
            if (manager != null && manager.GetLanguageDataBase() != null)
            {
                languages = manager.GetLanguageDataBase().EnabledLanguages.ToList();
            }
        }

        private void OnGUI()
        {
            GUILayout.Label("📑 Text Tab Editor", EditorStyles.boldLabel);
            GUILayout.Space(5);

            DrawSheetSelector();

            if (selectedSheetIndex >= 0 && selectedSheetIndex < sheets.Count)
            {
                GUILayout.Space(10);
                DrawSheetEditor(sheets[selectedSheetIndex]);
                DrawBottomEditor(sheets[selectedSheetIndex]);
            }

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("🔁 Refresh Languages"))
            {
                LoadLanguages();
                csvManager.import(ref sheets);
            }
            if (GUILayout.Button("💾 Export to CSV"))
            {
                csvManager.export(ref sheets, ref languages);
            }
            GUILayout.EndHorizontal();
        }

        private void DrawSheetSelector()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Sheets:", GUILayout.Width(50));

            if (GUILayout.Button("+", GUILayout.Width(25)))
            {
                AddSheet("Sheet " + (sheets.Count + 1));
            }
            if(GUILayout.Button("-", GUILayout.Width(25)) && sheets.Count > 0 && selectedSheetIndex >= 0)
            {
                if (EditorUtility.DisplayDialog("Delete Sheet", "Are you sure you want to delete this sheet?", "Yes", "No"))
                {
                    sheets.RemoveAt(selectedSheetIndex);
                    selectedSheetIndex = -1;
                }
            }

            if (sheets.Count > 0)
            {
                string[] sheetNames = sheets.ConvertAll(s => s.Name).ToArray();
                selectedSheetIndex = EditorGUILayout.Popup(selectedSheetIndex, sheetNames);

                if (selectedSheetIndex >= 0 && selectedSheetIndex < sheets.Count)
                {
                    GUILayout.Space(10);
                    GUILayout.Label("Rename:", GUILayout.Width(55));
                    string oldName = sheets[selectedSheetIndex].Name;
                    string newName = EditorGUILayout.TextField(oldName);

                    if (newName != oldName && !string.IsNullOrWhiteSpace(newName))
                    {
                        string folderPath = Path.Combine(Application.dataPath, "Translations/CSV");
                        string oldPath = Path.Combine(folderPath, oldName + ".csv");
                        string newPath = Path.Combine(folderPath, newName + ".csv");

                        if (File.Exists(oldPath) && !File.Exists(newPath))
                        {
                            string unityOldPath = "Assets/Translations/CSV/" + oldName + ".csv";
                            string unityNewPath = "Assets/Translations/CSV/" + newName + ".csv";

                            if (AssetDatabase.MoveAsset(unityOldPath, unityNewPath) == string.Empty)
                            {
                                sheets[selectedSheetIndex].Name = newName;
                            }
                            else
                            {
                                Debug.LogError("❌ Failed to rename CSV file using AssetDatabase.");
                            }

                        }

                        sheets[selectedSheetIndex].Name = newName;
                    }
                }
            }

            GUILayout.EndHorizontal();
        }

        private void DrawSheetEditor(SheetTemplate sheet)
        {
            GUILayout.Label("✏️ Editing: " + sheet.Name, EditorStyles.boldLabel);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(position.height * 0.6f));

            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Key", GUILayout.Width(200));
            foreach (var lang in languages)
            {
                GUILayout.Label(lang.ToString(), GUILayout.Width(150));
            }
            GUILayout.EndHorizontal();

            int? rowToRemove = null;

            for (int i = 0; i < sheet.Rows.Count; i++)
            {
                GUILayout.BeginHorizontal();
                sheet.Rows[i].key = EditorGUILayout.TextField(sheet.Rows[i].key, GUILayout.Width(200));

                while (sheet.Rows[i].cells.Count < languages.Count)
                    sheet.Rows[i].cells.Add(new CellTemplate());

                for (int j = 0; j < languages.Count; j++)
                {
                    string preview = string.IsNullOrEmpty(sheet.Rows[i].cells[j].text) ? "" : $"\"{sheet.Rows[i].cells[j].text}\"";
                    if (GUILayout.Button(preview, EditorStyles.textField, GUILayout.Width(150), GUILayout.Height(40)))
                    {
                        GUI.FocusControl(null);
                        selectedRow = i;
                        selectedColumn = j;
                    }
                }

                if (GUILayout.Button("-", GUILayout.Width(20)))
                {
                    rowToRemove = i;
                }

                GUILayout.EndHorizontal();
            }

            if (rowToRemove.HasValue)
            {
                sheet.Rows.RemoveAt(rowToRemove.Value);
                selectedRow = -1;
                selectedColumn = -1;
            }

            if (GUILayout.Button("Add Row"))
            {
                sheet.Rows.Add(new RowTemplate
                {
                    key = "new.key",
                    cells = languages.Select(l => new CellTemplate()).ToList()
                });
            }
            GUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        private void DrawBottomEditor(SheetTemplate sheet)
        {
            GUILayout.Space(10);
            GUILayout.Label("📝 Selected Cell Editor", EditorStyles.boldLabel);
            bottomScrollPos = EditorGUILayout.BeginScrollView(bottomScrollPos);

            if (selectedRow >= 0 && selectedRow < sheet.Rows.Count &&
                selectedColumn >= 0 && selectedColumn < sheet.Rows[selectedRow].cells.Count)
            {
                var current = sheet.Rows[selectedRow].cells[selectedColumn];
                GUILayout.Label("Preview: \"" + current.text + "\"");

                //TextRichEditor.DrawToolbar(updated =>
                //{
                //    shouldFocusTextArea = true;
                //    current.text = updated;
                //});

                //current.text = TextRichEditor.DrawEditableArea(current.text, ref shouldFocusTextArea);

                current.text = EditorGUILayout.TextArea(
                  current.text,
                  GUILayout.ExpandHeight(true),
                  GUILayout.ExpandWidth(true),
                  GUILayout.MinHeight(120));
            }
            else
            {
                GUILayout.Label("No cell selected.");
            }

            EditorGUILayout.EndScrollView();

        }

        private void AddSheet(string name)
        {
            var newSheet = new SheetTemplate
            {
                Name = name,
                Rows = new List<RowTemplate>()
            };
            sheets.Add(newSheet);
            selectedSheetIndex = sheets.Count - 1;
        }
    }

    [System.Serializable]
    public class SheetTemplate { public string Name; public List<RowTemplate> Rows = new(); }
    [System.Serializable]
    public class RowTemplate { public string key; public List<CellTemplate> cells = new(); }
    [System.Serializable]
    public class CellTemplate { public string text; }
}
