using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

namespace com.faolline.translationsystem
{
    public static class TranslationPrefabBrowser
    {
        [MenuItem("GameObject/Translation/Show All Prefabs", false, 0)]
        public static void ShowPrefabs()
        {
            // Rechercher tous les dossiers sous "Assets/Samples"
            string[] sampleRoots = AssetDatabase.GetSubFolders("Assets/Samples");

            // Collecte de tous les GUID de prefabs trouvés
            var allPrefabGUIDs = sampleRoots
                .SelectMany(folder => AssetDatabase.FindAssets("t:Prefab", new[] { folder }))
                .Distinct()
                .ToArray();

            if (allPrefabGUIDs.Length == 0)
            {
                EditorUtility.DisplayDialog("No Prefabs Found", "No prefabs found under Assets/Samples.", "OK");
                return;
            }

            GenericMenu menu = new GenericMenu();

            Debug.Log("Sample roots:");
            foreach (var folder in sampleRoots)
                Debug.Log($"- {folder}");

            foreach (string guid in allPrefabGUIDs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileNameWithoutExtension(path);
                 Debug.Log($"🧩 Found prefab: {fileName} at {path}");

                menu.AddItem(new GUIContent(fileName), false, () =>
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null)
                    {
                        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                        Undo.RegisterCreatedObjectUndo(instance, "Create " + fileName);
                        Selection.activeGameObject = instance;
                    }
                });
            }

            Debug.Log($"Found {allPrefabGUIDs.Length} prefab GUIDs.");

            var allFiles = Directory.GetFiles("Assets/Samples", "*.*", SearchOption.AllDirectories);
            foreach (var file in allFiles)
            {
                Debug.Log("Seen: " + file);
            }

            menu.ShowAsContext();
        }
    }
}
