using UnityEditor;
using UnityEngine;
using TMPro;

namespace com.faolline.translationsystem
{
    [CustomEditor(typeof(TranslateObjectDropdown))]
    public class TranslateObjectDropdownEditor : Editor
    {
        private TranslationKeyDatabase database;
        private int selectedCategoryIndex = 0;

        public override void OnInspectorGUI()
        {
            var dropdownTarget = (TranslateObjectDropdown)target;

            if (database == null)
            {
                string dbPath = "Assets/Translations/Generated/TranslationKeyDatabase.asset";
                database = AssetDatabase.LoadAssetAtPath<TranslationKeyDatabase>(dbPath);
            }

            if (database == null || database.GetCategories().Count == 0)
            {
                EditorGUILayout.HelpBox("No TranslationKeyDatabase found or it is empty.", MessageType.Warning);
                return;
            }

            // 🔹 Choix de la catégorie
            var categories = database.GetCategories();
            selectedCategoryIndex = Mathf.Clamp(selectedCategoryIndex, 0, categories.Count - 1);
            selectedCategoryIndex = EditorGUILayout.Popup("Category", selectedCategoryIndex, categories.ToArray());

            string selectedCategory = categories[selectedCategoryIndex];
            var keys = database.GetKeysInCategory(selectedCategory);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("📋 Dropdown Options", EditorStyles.boldLabel);

            // 🔹 Accès à la propriété sérialisée
            serializedObject.Update();
            SerializedProperty optionsProp = serializedObject.FindProperty("options");

            for (int i = 0; i < optionsProp.arraySize; i++)
            {
                SerializedProperty option = optionsProp.GetArrayElementAtIndex(i).FindPropertyRelative("translationKey");

                string currentKey = option.stringValue;
                int keyIndex = keys.IndexOf(currentKey);
                if (keyIndex < 0) keyIndex = 0;

                int newIndex = EditorGUILayout.Popup($"Option {i + 1}", keyIndex, keys.ToArray());

                if (newIndex != keyIndex)
                {
                    Undo.RecordObject(target, "Change Dropdown Translation Key");
                    string newKey = keys[newIndex];
                    option.stringValue = newKey;

                    // 🟢 Appliquer immédiatement la nouvelle liste
                    dropdownTarget.SetKey(i, newKey); // ⬅ tu dois ajouter cette méthode dans TranslateObjectDropdown
                    EditorUtility.SetDirty(target);

#if UNITY_EDITOR
                    TMP_Dropdown dropdown = dropdownTarget.GetComponent<TMP_Dropdown>();
                    if (dropdown != null)
                    {
                        UnityEditor.EditorUtility.SetDirty(dropdown);
                        UnityEditor.SceneView.RepaintAll();
                    }
#endif
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("➕ Add Option"))
            {
                optionsProp.InsertArrayElementAtIndex(optionsProp.arraySize);
                optionsProp.GetArrayElementAtIndex(optionsProp.arraySize - 1).FindPropertyRelative("translationKey").stringValue =
                    keys.Count > 0 ? keys[0] : "";
            }

            if (optionsProp.arraySize > 0 && GUILayout.Button("➖ Remove Last Option"))
            {
                optionsProp.DeleteArrayElementAtIndex(optionsProp.arraySize - 1);
            }
            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
