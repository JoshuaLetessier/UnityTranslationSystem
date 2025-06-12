using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace com.faolline.translationsystem
{
    public static class TextTabEditorUI
    {
        public static void DrawHeader()
        {
            GUILayout.Label("\ud83d\udcc1 Text Tab Editor", EditorStyles.boldLabel);
            GUILayout.Space(5);
        }

        public static void DrawSheetSelector(ref List<SheetTemplate> sheets, ref int selectedSheetIndex)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Sheets:", GUILayout.Width(50));

            if (GUILayout.Button("+", GUILayout.Width(25)))
            {
                var newSheet = new SheetTemplate { Name = "Sheet " + (sheets.Count + 1), Rows = new List<RowTemplate>() };
                sheets.Add(newSheet);
                selectedSheetIndex = sheets.Count - 1;
            }

            if (GUILayout.Button("-", GUILayout.Width(25)) && sheets.Count > 0 && selectedSheetIndex >= 0)
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
                        SheetManager.RenameSheet(sheets, selectedSheetIndex, newName);
                    }
                }
            }

            GUILayout.EndHorizontal();
        }

        public static void DrawSheetEditor(
            SheetTemplate sheet,
            ref int selectedRow,
            ref int selectedColumn,
            List<SupportedLanguage> languages,
            float height,
            Vector2 scrollPos,
            out Vector2 newScrollPos)
        {
            GUILayout.Label("\u270f\ufe0f Editing: " + sheet.Name, EditorStyles.boldLabel);
            newScrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(height));

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

                string currentKey = sheet.Rows[i].key;

                ValidationHelper.WithKeyValidationColor(currentKey, sheet.Rows, () =>
                {
                    sheet.Rows[i].key = EditorGUILayout.TextField(currentKey, GUILayout.Width(200));
                });

                ValidationHelper.ShowKeyValidation(currentKey, sheet.Rows);

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

        public static void DrawBottomEditor(
            SheetTemplate sheet,
            int selectedRow,
            int selectedColumn,
            float height,
            Vector2 scrollPos,
            out Vector2 newScrollPos)
        {
            GUILayout.Space(10);
            GUILayout.Label("\ud83d\udcdd Selected Cell Editor", EditorStyles.boldLabel);

            newScrollPos = scrollPos;

            if (selectedRow >= 0 && selectedRow < sheet.Rows.Count &&
                selectedColumn >= 0 && selectedColumn < sheet.Rows[selectedRow].cells.Count)
            {
                var current = sheet.Rows[selectedRow].cells[selectedColumn];

                TextRichEditor.DrawToolbar(updated => current.text = updated);

                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label("Preview:");
                newScrollPos = EditorGUILayout.BeginScrollView(newScrollPos, GUILayout.Height(height * 0.30f));
                GUILayout.Label($"\"{current.text}\"", EditorStyles.wordWrappedLabel);
                EditorGUILayout.EndScrollView();
                GUILayout.EndVertical();

                newScrollPos = EditorGUILayout.BeginScrollView(newScrollPos, GUILayout.Height(height * 0.70f));
                current.text = EditorGUILayout.TextArea(
                    current.text,
                    GUILayout.ExpandHeight(true),
                    GUILayout.ExpandWidth(true),
                    GUILayout.MinHeight(120));
                EditorGUILayout.EndScrollView();
            }
            else
            {
                newScrollPos = EditorGUILayout.BeginScrollView(newScrollPos, GUILayout.Height(height));
                GUILayout.Label("No cell selected.");
                EditorGUILayout.EndScrollView();
            }
        }

        public static bool DrawExportButton(List<SheetTemplate> sheets, List<SupportedLanguage> languages, CSVManager csvManager)
        {
            if (GUILayout.Button("\ud83d\udcbe Export to CSV"))
            {
                if (ValidationHelper.ContainsInvalidKeys(sheets))
                {
                    EditorUtility.DisplayDialog("Export Failed", "Please fix all key errors before exporting.", "OK");
                    return false;
                }

                csvManager.export(ref sheets, ref languages);
                return true;
            }
            return false;
        }
    }
}
