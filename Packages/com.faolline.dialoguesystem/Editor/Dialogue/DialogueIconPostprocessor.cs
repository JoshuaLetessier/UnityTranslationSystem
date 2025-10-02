using System;
using UnityEditor;
using UnityEngine;

namespace com.faolline.dialoguesystem
{
    public static class DialogueIconUtility
    {
        private const string IconPath = "Assets/Editor/Icons/dialogueIcon.png";
        private static Texture2D _icon;

        public static Texture2D Icon
        {
            get
            {
                if (_icon == null)
                    _icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
                return _icon;
            }
        }

        public static void TryApplyIconToAsset(Dialogue obj)
        {
            if (Icon == null || obj == null) return;
            EditorGUIUtility.SetIconForObject(obj, Icon);
            EditorUtility.SetDirty(obj);
        }
    }

    public class DialogueIconPostprocessor : AssetPostprocessor
    {
        // Déclenché à chaque import / suppression / déplacement / renommage d’asset
        static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            // Traite les nouveaux/renommés/reimportés
            ProcessPaths(importedAssets);
            ProcessPaths(movedAssets);
        }

        private static void ProcessPaths(string[] paths)
        {
            if (paths == null) return;

            foreach (var path in paths)
            {
                // Tente de charger un Dialogue à ce chemin
                var asset = AssetDatabase.LoadAssetAtPath<Dialogue>(path);
                if (asset != null)
                {
                    DialogueIconUtility.TryApplyIconToAsset(asset);
                }
            }

            AssetDatabase.SaveAssets();
            // Pas de Refresh agressif ici : Unity rafraîchit déjà la vue
        }
    }

    // Optionnel : bouton utilitaire pour (ré)appliquer sur tout le projet
    public static class DialogueIconTools
    {
        [MenuItem("Tools/Dialogue/Apply Icons To All")]
        public static void ApplyIconsToAll()
        {
            var guids = AssetDatabase.FindAssets("t:Dialogue");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<Dialogue>(path);
                if (asset != null)
                    DialogueIconUtility.TryApplyIconToAsset(asset);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Dialogue icons reapplied to all Dialogue assets.");
        }
    }
}
