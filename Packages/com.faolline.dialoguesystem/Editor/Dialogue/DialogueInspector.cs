using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace com.faolline.dialoguesystem
{
    // Inspector custom avec bouton "Open Graph"
    [CustomEditor(typeof(Dialogue))]
    public class DialogueInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            if (GUILayout.Button("Open Graph"))
            {
                DialogueWindowEditor.Open((Dialogue)target);
            }
        }
    }

    // Double-clic sur l’asset -> ouvre la fenêtre liée
    public static class DialogueAssetOpener
    {
        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceId, int line)
        {
            var obj = EditorUtility.InstanceIDToObject(instanceId);
            if (obj is Dialogue dialogue)
            {
                DialogueWindowEditor.Open(dialogue);
                return true; // on a géré l’ouverture
            }
            return false;
        }
    }

    // Menu utilitaire pour ouvrir le Dialogue sélectionné
    public static class DialogueWindowMenu
    {
        [MenuItem("Window/Dialogue Graph/Open Selected", priority = 2000)]
        public static void OpenSelected()
        {
            if (Selection.activeObject is Dialogue d)
                DialogueWindowEditor.Open(d);
            else
                EditorUtility.DisplayDialog("Dialogue Graph", "Sélectionne un asset Dialogue.", "OK");
        }

        [MenuItem("Window/Dialogue Graph/Open Selected", validate = true)]
        public static bool ValidateOpenSelected() => Selection.activeObject is Dialogue;
    }
}
