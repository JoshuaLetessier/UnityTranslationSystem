using UnityEditor;
using UnityEngine;
using com.faolline.translationsystem;

namespace com.faolline.translationsystem
{
    [CustomEditor(typeof(TranslateObjectImage))]
    public class TranslateObjectImageEditor : Editor
    {
        SerializedProperty translationsProp;
        SerializedProperty defaultSpriteProp;

        private void OnEnable()
        {
            translationsProp = serializedObject.FindProperty("translations");
            defaultSpriteProp = serializedObject.FindProperty("defaultSprite");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("🖼 Image Translations", EditorStyles.boldLabel);

            for (int i = 0; i < translationsProp.arraySize; i++)
            {
                SerializedProperty element = translationsProp.GetArrayElementAtIndex(i);
                SerializedProperty langProp = element.FindPropertyRelative("language");
                SerializedProperty spriteProp = element.FindPropertyRelative("sprite");

                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.PropertyField(langProp, GUIContent.none, GUILayout.MaxWidth(120));
                EditorGUILayout.PropertyField(spriteProp, GUIContent.none);

                if (GUILayout.Button("-", GUILayout.Width(20)))
                {
                    translationsProp.DeleteArrayElementAtIndex(i);
                    break;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("+ Add Translation"))
            {
                translationsProp.InsertArrayElementAtIndex(translationsProp.arraySize);
                SerializedProperty newElement = translationsProp.GetArrayElementAtIndex(translationsProp.arraySize - 1);
                newElement.FindPropertyRelative("language").enumValueIndex = 0;
                newElement.FindPropertyRelative("sprite").objectReferenceValue = null;
            }

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(defaultSpriteProp, new GUIContent("Fallback Sprite"));

            serializedObject.ApplyModifiedProperties();
        }
    }
}
