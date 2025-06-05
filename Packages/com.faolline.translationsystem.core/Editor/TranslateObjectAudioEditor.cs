using UnityEditor;
using UnityEngine;
using com.faolline.translationsystem;

namespace com.faolline.translationsystem
{
    [CustomEditor(typeof(TranslateObjectAudio))]
    public class TranslateObjectAudioEditor : Editor
    {
        SerializedProperty translationsProp;

        private void OnEnable()
        {
            translationsProp = serializedObject.FindProperty("translations");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("\uD83C\uDFA7 Audio Translations", EditorStyles.boldLabel);

            for (int i = 0; i < translationsProp.arraySize; i++)
            {
                SerializedProperty element = translationsProp.GetArrayElementAtIndex(i);
                SerializedProperty langProp = element.FindPropertyRelative("language");
                SerializedProperty clipProp = element.FindPropertyRelative("clip");

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(langProp, GUIContent.none, GUILayout.MaxWidth(120));
                EditorGUILayout.PropertyField(clipProp, GUIContent.none);

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
                var newElement = translationsProp.GetArrayElementAtIndex(translationsProp.arraySize - 1);
                newElement.FindPropertyRelative("language").enumValueIndex = 0;
                newElement.FindPropertyRelative("clip").objectReferenceValue = null;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
