using com.faolline.translationsystem;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TranslateObjectText))]
public class TranslateObjectTextEditor : Editor
{
    private TranslationKeyDatabase database;
    private int selectedCategoryIndex = 0;
    private int selectedKeyIndex = 0;

    public override void OnInspectorGUI()
    {
        var targetScript = (TranslateObjectText)target;

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

        var categories = database.GetCategories();
        selectedCategoryIndex = Mathf.Clamp(selectedCategoryIndex, 0, categories.Count - 1);
        selectedCategoryIndex = EditorGUILayout.Popup("Category", selectedCategoryIndex, categories.ToArray());

        string selectedCategory = categories[selectedCategoryIndex];
        var keys = database.GetKeysInCategory(selectedCategory);

        // Cherche l'index courant
        int currentKeyIndex = keys.IndexOf(targetScript.GetKey());
        if (currentKeyIndex < 0) currentKeyIndex = 0;

        selectedKeyIndex = EditorGUILayout.Popup("Translation Key", currentKeyIndex, keys.ToArray());

        string newKey = keys[selectedKeyIndex];
        if (newKey != targetScript.GetKey())
        {
            Undo.RecordObject(target, "Change Translation Key");
            targetScript.SetKey(newKey);
            EditorUtility.SetDirty(targetScript);
        }

        // Affiche autres propriétés
        DrawPropertiesExcluding(serializedObject, "translationKey");
    }
}
