using UnityEditor;
using UnityEngine;
using com.faolline.translationsystem;
using System.Linq;

namespace com.faolline.translationsystem
{
    [CustomEditor(typeof(LanguageManager))]
    public class LanguageManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector(); // Affiche les autres propriétés normalement

            LanguageManager manager = (LanguageManager)target;

            if (manager == null || manager.GetLanguageDataBase() == null)
            {
                EditorGUILayout.HelpBox("LanguageDataBase not assigned.", MessageType.Error);
                return;
            }

            var db = manager.GetLanguageDataBase();
            var enabledLanguages = db.EnabledLanguages;

            if (enabledLanguages == null || enabledLanguages.Count == 0)
            {
                EditorGUILayout.HelpBox("No enabled languages found in LanguageDataBase.", MessageType.Warning);
                return;
            }

            // Rendu de l'éditeur uniquement si en PlayMode
            if (Application.isPlaying)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("🔁 Debug Language Switch (Play Mode)", EditorStyles.boldLabel);

                string[] languageNames = enabledLanguages.Select(lang => lang.GetNativeName()).ToArray();
                int currentIndex = enabledLanguages.ToList().IndexOf(manager.GetCurrentLanguage());
                if (currentIndex < 0) currentIndex = 0;

                int selectedIndex = EditorGUILayout.Popup("Current Language", currentIndex, languageNames);

                if (selectedIndex != currentIndex)
                {
                    SupportedLanguage selectedLang = enabledLanguages[selectedIndex];
                    manager.ChangeLanguage(selectedLang.ToString());
                }
            }

            // Sélecteur de langue par défaut (même hors PlayMode)
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("⚙️ Configuration", EditorStyles.boldLabel);

            SupportedLanguage currentDefault = manager.GetDefaultLanguage();

            int defaultIndex = enabledLanguages.ToList().IndexOf(currentDefault);
            if (defaultIndex < 0) defaultIndex = 0;

            string[] displayNames = enabledLanguages
                .Select(lang => lang.GetNativeName() + $" ({lang})").ToArray();

            int newDefaultIndex = EditorGUILayout.Popup("Default Language", defaultIndex, displayNames);

            if (newDefaultIndex != defaultIndex)
            {
                SupportedLanguage newLang = enabledLanguages[newDefaultIndex];
                manager.SetDefaultLanguage(newLang);
                EditorUtility.SetDirty(manager); // Pour marquer l'objet comme modifié
            }

        }
    }

}
