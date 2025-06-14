using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace com.faolline.translationsystem
{
    public static class ValidationHelper
    {
        public static bool ContainsInvalidKeys(List<SheetTemplate> sheets)
        {
            HashSet<string> seenKeys = new();

            foreach (var sheet in sheets)
            {
                foreach (var row in sheet.Rows)
                {
                    if (string.IsNullOrWhiteSpace(row.key))
                        return true;
                    if (!seenKeys.Add(row.key))
                        return true;
                }
            }
            return false;
        }

        public static bool IsKeyDuplicate(string key, List<RowTemplate> rows)
        {
            return rows.Count(r => r.key == key) > 1;
        }

        public static bool IsKeyEmpty(string key)
        {
            return string.IsNullOrWhiteSpace(key);
        }

        public static void ShowKeyValidation(string key, List<RowTemplate> rows)
        {
            if (IsKeyEmpty(key))
            {
                EditorGUILayout.HelpBox("Key is empty!", MessageType.Error);
            }
            else if (IsKeyDuplicate(key, rows))
            {
                EditorGUILayout.HelpBox("Duplicate key!", MessageType.Warning);
            }
        }

        public static void WithKeyValidationColor(string key, List<RowTemplate> rows, System.Action drawField)
        {
            Color oldColor = GUI.color;
            if (IsKeyEmpty(key) || IsKeyDuplicate(key, rows))
                GUI.color = Color.red;

            drawField.Invoke();
            GUI.color = oldColor;
        }
    }
}
