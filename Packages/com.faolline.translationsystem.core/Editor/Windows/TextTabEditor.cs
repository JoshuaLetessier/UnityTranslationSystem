using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

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
            LoadLanguagesAndSheets();

            if (sheets.Count == 0)
                SheetManager.AddSheet(sheets, "Default");
        }

        private void LoadLanguagesAndSheets()
        {
            languages = LanguageLoader.LoadLanguages();
            csvManager.import(ref sheets);
        }

        private void OnGUI()
        {
            GUILayout.BeginVertical();

            TextTabEditorUI.DrawHeader();
            TextTabEditorUI.DrawSheetSelector(ref sheets, ref selectedSheetIndex);

            if (selectedSheetIndex >= 0 && selectedSheetIndex < sheets.Count)
            {
                GUILayout.Space(10);

                float availableHeight = position.height;
                float controlHeight = 90;
                float sheetEditorHeight = availableHeight * 0.40f;
                float bottomEditorHeight = availableHeight - sheetEditorHeight - controlHeight - 100;

                TextTabEditorUI.DrawSheetEditor(sheets[selectedSheetIndex], ref selectedRow, ref selectedColumn, languages, sheetEditorHeight, scrollPos, out scrollPos);
                TextTabEditorUI.DrawBottomEditor(sheets[selectedSheetIndex], selectedRow, selectedColumn, bottomEditorHeight, bottomScrollPos, out bottomScrollPos);
            }

            GUILayout.FlexibleSpace();
            GUILayout.Space(10);
            DrawBottomButtons();
            GUILayout.EndVertical();
        }

        private void DrawBottomButtons()
        {
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("\ud83d\udd01 Refresh Languages"))
            {
                LoadLanguagesAndSheets();
            }

            TextTabEditorUI.DrawExportButton(sheets, languages, csvManager);

            GUILayout.EndHorizontal();
        }
    }

    [System.Serializable]
    public class SheetTemplate { public string Name; public List<RowTemplate> Rows = new(); }
    [System.Serializable]
    public class RowTemplate { public string key; public List<CellTemplate> cells = new(); }
    [System.Serializable]
    public class CellTemplate { public string text; }
}
