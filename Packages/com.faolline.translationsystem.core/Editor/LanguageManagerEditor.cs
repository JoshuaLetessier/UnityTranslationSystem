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
            DrawDefaultInspector();

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

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("🌍 Current Language", EditorStyles.boldLabel);

            string[] languageNames = enabledLanguages.Select(lang => lang.GetNativeName()).ToArray();
            int currentIndex = enabledLanguages.ToList().IndexOf(manager.GetCurrentLanguage()); // Convert IReadOnlyList to List

            if (currentIndex < 0) currentIndex = 0;

            int selectedIndex = EditorGUILayout.Popup("Current Language", currentIndex, languageNames);

            if (selectedIndex != currentIndex)
            {
                SupportedLanguage selectedLang = enabledLanguages[selectedIndex];
                manager.ForceSetCurrentLanguage(selectedLang); // nouvelle méthode pour Editor
                EditorUtility.SetDirty(manager);
            }

            // Sélecteur de langue par défaut
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("⚙️ Configuration", EditorStyles.boldLabel);

            SupportedLanguage currentDefault = manager.GetDefaultLanguage();
            int defaultIndex = enabledLanguages.ToList().IndexOf(currentDefault); // Convert IReadOnlyList to List

            if (defaultIndex < 0) defaultIndex = 0;

            string[] displayNames = enabledLanguages
                .Select(lang => lang.GetNativeName() + $" ({lang})").ToArray();

            int newDefaultIndex = EditorGUILayout.Popup("Default Language", defaultIndex, displayNames);

            if (newDefaultIndex != defaultIndex)
            {
                SupportedLanguage newLang = enabledLanguages[newDefaultIndex];
                manager.SetDefaultLanguage(newLang);
                EditorUtility.SetDirty(manager);
            }
        }
    }
}
