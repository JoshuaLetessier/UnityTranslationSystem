using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using com.faolline.translationsystem;

namespace com.faolline.dialoguesystem
{
    [CustomEditor(typeof(Dialogue))]
    public class DialogueEditor : Editor
    {
        private SerializedProperty _titleProp;
        private SerializedProperty _versionProp;
        private SerializedProperty _speakersProp;
        private SerializedProperty _startSentenceIdProp;
        private SerializedProperty _allTranslationKeysProp;

        private ReorderableList _speakersList;

        private TranslationKeyDatabase _database;
        private int _selectedCategoryIndex;
        private int _lastCategoryIndex = -1;     // pour ne rafraîchir la liste que si la catégorie change

        private void OnEnable()
        {
            _titleProp = serializedObject.FindProperty("title");
            _versionProp = serializedObject.FindProperty("version");
            _speakersProp = serializedObject.FindProperty("speakers");
            _startSentenceIdProp = serializedObject.FindProperty("startSentenceId");
            _allTranslationKeysProp = serializedObject.FindProperty("allTranslationKeys"); // OK: array/list

            _speakersList = new ReorderableList(serializedObject, _speakersProp, true, true, true, true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Speakers"),
                drawElementCallback = (rect, index, active, focused) =>
                {
                    var element = _speakersProp.GetArrayElementAtIndex(index);
                    rect.y += 2;
                    EditorGUI.PropertyField(
                        new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
                        element, GUIContent.none);
                }
            };

            // Lookup souple de la DB
            var guids = AssetDatabase.FindAssets("t:TranslationKeyDatabase");
            if (guids != null && guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                _database = AssetDatabase.LoadAssetAtPath<TranslationKeyDatabase>(path);
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_titleProp);
            EditorGUILayout.PropertyField(_versionProp);

            EditorGUILayout.Space(6);
            _speakersList.DoLayoutList();

            EditorGUILayout.Space(6);
            EditorGUILayout.PropertyField(_startSentenceIdProp, new GUIContent("Start Sentence Id"));

            EditorGUILayout.Space(8);
            DrawTranslationCategoryUI(); // met à jour _allTranslationKeysProp si besoin

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawTranslationCategoryUI()
        {
            EditorGUILayout.LabelField("Translations", EditorStyles.boldLabel);

            if (_database == null)
            {
                EditorGUILayout.HelpBox("No TranslationKeyDatabase found in project.", MessageType.Warning);
                if (GUILayout.Button("Refresh Lookup"))
                    OnEnable(); // retente le lookup
                return;
            }

            var categories = _database.GetCategories();
            if (categories == null || categories.Count == 0)
            {
                EditorGUILayout.HelpBox("TranslationKeyDatabase is empty.", MessageType.Info);
                return;
            }

            _selectedCategoryIndex = Mathf.Clamp(_selectedCategoryIndex, 0, categories.Count - 1);
            int newIndex = EditorGUILayout.Popup("Category", _selectedCategoryIndex, categories.ToArray());

            // Bouton manuel pour re-synchroniser si nécessaire
            //bool forceSync = GUILayout.Button("Sync keys from selected category");

            if (newIndex != _selectedCategoryIndex /*|| forceSync*/)
            {
                _selectedCategoryIndex = newIndex;
                _lastCategoryIndex = _selectedCategoryIndex;

                string selectedCategory = categories[_selectedCategoryIndex];
                var keys = _database.GetKeysInCategory(selectedCategory) ?? new List<string>();

                // Écrit proprement la LIST<string> sérialisée
                _allTranslationKeysProp.ClearArray();
                for (int i = 0; i < keys.Count; i++)
                {
                    _allTranslationKeysProp.InsertArrayElementAtIndex(i);
                    _allTranslationKeysProp.GetArrayElementAtIndex(i).stringValue = keys[i];
                }

                // Persistance asset
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
            }

            if (EditorGUILayout.LinkButton("Open Translation Database"))
            {
                Selection.activeObject = _database;
                EditorGUIUtility.PingObject(_database);
            }
        }
    }
}
