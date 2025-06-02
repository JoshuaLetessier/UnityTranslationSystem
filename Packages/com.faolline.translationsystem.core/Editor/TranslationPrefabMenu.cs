using System.IO;
using UnityEditor;
using UnityEngine;

namespace com.faolline.translationsystem
{
    public static class TranslationPrefabMenu
    {
        [MenuItem("GameObject/Translation/Show Available Prefabs", false, 0)]
        public static void ShowTranslationPrefabs()
        {
            const string basePath = "Assets/Samples/Translation System";

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { basePath });
            if (guids.Length == 0)
            {
                EditorUtility.DisplayDialog("No Prefabs Found", "No prefabs were found in the package Samples~ folder.", "OK");
                return;
            }

            GenericMenu menu = new GenericMenu();

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileNameWithoutExtension(assetPath);

                menu.AddItem(new GUIContent(fileName), false, () =>
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                    if (prefab != null)
                    {
                        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                        Undo.RegisterCreatedObjectUndo(instance, "Create " + fileName);
                        Selection.activeGameObject = instance;
                    }
                });
            }

            menu.ShowAsContext();
        }
    }
}
