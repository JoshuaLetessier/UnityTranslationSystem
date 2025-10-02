using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace com.faolline.dialoguesystem
{
    [CustomEditor(typeof(Speaker))]
    public class SpeakerEditor : Editor
    {
        // ----- Speaker base -----
        SerializedProperty _idProp;                 // speakerName (logical ID)
        SerializedProperty _displayModeProp;        // displayNameMode
        SerializedProperty _displayLiteralProp;     // displayNameLiteral
        SerializedProperty _displayKeyProp;         // displayNameKey

        // Optionnel: si tu ajoutes un fallback global
        SerializedProperty _defaultPrefabProp;      // defaultPrefab (GameObject) — peut être null si pas dans le modèle

        // Expressions : List<SpeakerExpression{ key, displayName?, prefab }>
        SerializedProperty _expressionsProp;
        ReorderableList _expressionsRL;

        // UI state
        readonly Dictionary<string, bool> _foldoutByPath = new();

        // Translation DB
        TranslationKeyDatabase _db;
        int _catIndex;
        int _keyIndex;
        List<string> _cats;
        List<string> _keys;

        void OnEnable()
        {
            _idProp = serializedObject.FindProperty("speakerName");
            _displayModeProp = serializedObject.FindProperty("displayNameMode");
            _displayLiteralProp = serializedObject.FindProperty("displayNameLiteral");
            _displayKeyProp = serializedObject.FindProperty("displayNameKey");
            _defaultPrefabProp = serializedObject.FindProperty("defaultPrefab");    // peut être null (pas obligatoire)
            _expressionsProp = serializedObject.FindProperty("expressions");

            BuildExpressionsList();

            // Lookup DB (optionnel)
            var guids = AssetDatabase.FindAssets("t:TranslationKeyDatabase");
            if (guids != null && guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                _db = AssetDatabase.LoadAssetAtPath<TranslationKeyDatabase>(path);
            }
            RefreshCategories();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // ----- Logical ID -----
            EditorGUILayout.LabelField("Logical ID", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_idProp, new GUIContent("Speaker ID"));
            EditorGUILayout.Space(8);

            // ----- Display Name -----
            EditorGUILayout.LabelField("Display Name", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_displayModeProp, new GUIContent("Mode"));
            var mode = (NameDisplayMode)_displayModeProp.enumValueIndex;
            if (mode == NameDisplayMode.Literal)
            {
                EditorGUILayout.PropertyField(_displayLiteralProp, new GUIContent("Text"));
            }
            else
            {
                DrawDisplayNameKeyPicker();
            }
            EditorGUILayout.Space(8);

            // ----- Default Prefab (optionnel) -----
            if (_defaultPrefabProp != null)
            {
                EditorGUILayout.LabelField("Defaults / Fallback", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(_defaultPrefabProp, new GUIContent("Default Prefab"));
                EditorGUILayout.Space(8);
            }

            // ----- Expressions -----
            EditorGUILayout.LabelField("Expressions", EditorStyles.boldLabel);
            _expressionsRL.DoLayoutList();

            if (serializedObject.ApplyModifiedProperties())
            {
#if UNITY_EDITOR
                // garde ta synchro d’ID si tu l’utilises
                var sp = (Speaker)target;
                var mi = typeof(Speaker).GetMethod("__EditorForceSyncId", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (mi != null) mi.Invoke(sp, null);
                EditorUtility.SetDirty(target);
#endif
            }
        }

        // ---------------------------------------------------------------------
        // Expressions RL : un GameObject par expression
        // ---------------------------------------------------------------------
        void BuildExpressionsList()
        {
            _expressionsRL = new ReorderableList(serializedObject, _expressionsProp, true, true, true, true);

            _expressionsRL.drawHeaderCallback = r => EditorGUI.LabelField(r, "Expressions (key → prefab)");

            _expressionsRL.elementHeightCallback = index =>
            {
                var elem = _expressionsProp.GetArrayElementAtIndex(index);
                var path = elem.propertyPath;
                bool open = _foldoutByPath.TryGetValue(path, out var v) && v;

                // ligne header
                float h = EditorGUIUtility.singleLineHeight + 6f;

                if (open)
                {
                    // displayName ? (optionnel si présent dans le modèle)
                    var displayName = elem.FindPropertyRelative("displayName");
                    if (displayName != null)
                        h += EditorGUIUtility.singleLineHeight + 4f;

                    // prefab
                    h += EditorGUIUtility.singleLineHeight + 4f;
                }
                return h;
            };

            _expressionsRL.drawElementCallback = (rect, index, active, focused) =>
            {
                var elem = _expressionsProp.GetArrayElementAtIndex(index);
                var keyProp = elem.FindPropertyRelative("key");
                var displayName = elem.FindPropertyRelative("displayName"); // peut être null si supprimé du modèle
                var prefabProp = elem.FindPropertyRelative("prefab");      // GameObject

                var path = elem.propertyPath;
                bool open = _foldoutByPath.TryGetValue(path, out var v) && v;

                // Header: foldout + Key
                var head = rect; head.height = EditorGUIUtility.singleLineHeight;
                var foldRect = head; foldRect.width = 16f;
                var keyRect = head; keyRect.x += 18f; keyRect.width -= 18f;

                bool newOpen = EditorGUI.Foldout(foldRect, open, GUIContent.none);
                if (newOpen != open) _foldoutByPath[path] = newOpen;

                keyProp.stringValue = EditorGUI.TextField(keyRect, "Key", keyProp.stringValue);

                if (!newOpen) return;

                float y = head.yMax + 4f;

                if (displayName != null)
                {
                    var dnRect = new Rect(rect.x + 14f, y, rect.width - 14f, EditorGUIUtility.singleLineHeight);
                    displayName.stringValue = EditorGUI.TextField(dnRect, "Display (optional)", displayName.stringValue);
                    y += EditorGUIUtility.singleLineHeight + 4f;
                }

                var pfRect = new Rect(rect.x + 14f, y, rect.width - 14f, EditorGUIUtility.singleLineHeight);
                EditorGUI.ObjectField(pfRect, prefabProp, new GUIContent("Prefab"));
            };

            _expressionsRL.onAddCallback = _ =>
            {
                _expressionsProp.arraySize++;
                var elem = _expressionsProp.GetArrayElementAtIndex(_expressionsProp.arraySize - 1);
                var keyProp = elem.FindPropertyRelative("key");
                var displayName = elem.FindPropertyRelative("displayName");
                var prefabProp = elem.FindPropertyRelative("prefab");

                if (keyProp != null) keyProp.stringValue = "new_expression";
                if (displayName != null) displayName.stringValue = string.Empty;
                if (prefabProp != null) prefabProp.objectReferenceValue = null;

                serializedObject.ApplyModifiedProperties();
            };
        }

        // ---------------------------------------------------------------------
        // Translation DB helpers (Display Name)
        // ---------------------------------------------------------------------
        void RefreshCategories()
        {
            _cats = null; _keys = null;
            if (_db != null)
            {
                _cats = _db.GetCategories() ?? new List<string>();
                _catIndex = Mathf.Clamp(_catIndex, 0, Mathf.Max(0, _cats.Count - 1));
                RefreshKeys();
            }
        }

        void RefreshKeys()
        {
            _keys = new List<string>();
            if (_db == null || _cats == null || _cats.Count == 0) return;

            var cat = _cats[Mathf.Clamp(_catIndex, 0, _cats.Count - 1)];
            var list = _db.GetKeysInCategory(cat) ?? new List<string>();
            _keys.AddRange(list);

            // Reselect by existing key if possible
            var currentKey = _displayKeyProp.stringValue;
            _keyIndex = Mathf.Max(0, _keys.IndexOf(currentKey));
        }

        void DrawDisplayNameKeyPicker()
        {
            if (_db == null)
            {
                EditorGUILayout.HelpBox("No TranslationKeyDatabase found.", MessageType.Warning);
                if (GUILayout.Button("Refresh Lookup")) OnEnable();
                return;
            }
            if (_cats == null || _cats.Count == 0)
            {
                EditorGUILayout.HelpBox("TranslationKeyDatabase is empty.", MessageType.Info);
                if (GUILayout.Button("Open Translation Database"))
                {
                    Selection.activeObject = _db;
                    EditorGUIUtility.PingObject(_db);
                }
                return;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Category", GUILayout.Width(70));
            var newCat = EditorGUILayout.Popup(_catIndex, _cats.ToArray());
            EditorGUILayout.EndHorizontal();

            if (newCat != _catIndex)
            {
                _catIndex = newCat;
                RefreshKeys();
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Key", GUILayout.Width(70));
            if (_keys != null && _keys.Count > 0)
            {
                var newKey = EditorGUILayout.Popup(_keyIndex, _keys.ToArray());
                if (newKey != _keyIndex)
                {
                    _keyIndex = newKey;
                    _displayKeyProp.stringValue = _keys[_keyIndex];
                    serializedObject.ApplyModifiedProperties();

#if UNITY_EDITOR
                    var sp = (Speaker)target;
                    var mi = typeof(Speaker).GetMethod("__EditorForceSyncId", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                    if (mi != null) mi.Invoke(sp, null);
                    EditorUtility.SetDirty(target);
                    AssetDatabase.SaveAssets();
#endif
                }
            }
            else
            {
                EditorGUILayout.LabelField("(no keys)");
            }
            EditorGUILayout.EndHorizontal();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Open Translation Database"))
                {
                    Selection.activeObject = _db;
                    EditorGUIUtility.PingObject(_db);
                }
                if (GUILayout.Button("Refresh"))
                {
                    RefreshCategories();
                }
            }
        }
    }
}
