using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

namespace com.faolline.translationsystem
{
    public static class TranslationPrefabBrowser
    {
        private const string generatedFilePath = "Assets/UnityTranslationSystem/Editor/GeneratedTranslationMenu.cs";

        public static void GenerateMenuFromPrefabs()
        {
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Samples" });
            if (prefabGuids.Length == 0)
            {
                Debug.LogWarning("No prefabs found in Samples. No menu generated.");
                return;
            }

            string classCode = @"using UnityEditor;
using UnityEngine;

namespace com.faolline.translationsystem.generated
{
    public static class GeneratedTranslationMenu
    {
";

            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileNameWithoutExtension(path);
                string safeMethodName = fileName.Replace(" ", "").Replace("(", "").Replace(")", "").Replace("-", "_");

                classCode +=
                    "        [MenuItem(\"GameObject/Translation/" + fileName + "\", false, 10)]\n" +
                    "        public static void Create_" + safeMethodName + "()\n" +
                    "        {\n" +
                    "            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(\"" + path.Replace("\\", "/") + "\");\n" +
                    "            if (prefab != null)\n" +
                    "            {\n" +
                    "                var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;\n" +
                    "                Undo.RegisterCreatedObjectUndo(instance, \"Create " + fileName + "\");\n" +
                    "                Selection.activeGameObject = instance;\n" +
                    "            }\n" +
                    "        }\n\n";
            }

            classCode += @"    }
}";

            // Assure le dossier
            string dir = Path.GetDirectoryName(generatedFilePath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            File.WriteAllText(generatedFilePath, classCode);
            AssetDatabase.Refresh();

            Debug.Log($"✅ Generated static menu for {prefabGuids.Length} prefabs at: {generatedFilePath}");
                    }
                }
}
