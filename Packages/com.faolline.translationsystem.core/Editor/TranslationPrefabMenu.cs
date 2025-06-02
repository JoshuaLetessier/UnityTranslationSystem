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

            foreach (string guid in allPrefabGUIDs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileNameWithoutExtension(path);

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

            menu.ShowAsContext();
        }
    }
}
