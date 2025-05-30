using UnityEditor;
using com.faolline.translationsystem;

namespace com.faolline.translationsystem
{
    [CustomEditor(typeof(LanguageDataBase))]
    public class LanguageDataBaseEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var db = (LanguageDataBase)target;
            EditorGUILayout.LabelField("Available Languages", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            foreach (SupportedLanguage lang in System.Enum.GetValues(typeof(SupportedLanguage)))
            {
                bool current = db.IsLanguageEnabled(lang);
                bool updated = EditorGUILayout.ToggleLeft(lang.GetNativeName(), current);

                if (updated != current)
                    db.SetLanguageEnabled(lang, updated);
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(db);
            }
        }
    }
}
