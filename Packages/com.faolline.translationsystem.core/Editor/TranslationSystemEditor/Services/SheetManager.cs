using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace com.faolline.translationsystem
{
    public static class SheetManager
    {
        public static void AddSheet(List<SheetTemplate> sheets, string name)
        {
            sheets.Add(new SheetTemplate { Name = name, Rows = new List<RowTemplate>() });
        }

        public static void RemoveSheet(List<SheetTemplate> sheets, int index)
        {
            if (index >= 0 && index < sheets.Count)
                sheets.RemoveAt(index);
        }

        public static void RenameSheet(List<SheetTemplate> sheets, int index, string newName)
        {
            if (index < 0 || index >= sheets.Count) return;

            string oldName = sheets[index].Name;
            string folderPath = Path.Combine(Application.dataPath, "Translations/CSV");
            string oldPath = Path.Combine(folderPath, oldName + ".csv");
            string newPath = Path.Combine(folderPath, newName + ".csv");

            if (File.Exists(oldPath) && !File.Exists(newPath))
            {
                string unityOldPath = "Assets/Translations/CSV/" + oldName + ".csv";
                string unityNewPath = "Assets/Translations/CSV/" + newName + ".csv";

                if (AssetDatabase.MoveAsset(unityOldPath, unityNewPath) == string.Empty)
                {
                    sheets[index].Name = newName;
                }
                else
                {
                    Debug.LogError("❌ Failed to rename CSV file using AssetDatabase.");
                }
            }
            else
            {
                sheets[index].Name = newName;
            }
        }
    }
}
