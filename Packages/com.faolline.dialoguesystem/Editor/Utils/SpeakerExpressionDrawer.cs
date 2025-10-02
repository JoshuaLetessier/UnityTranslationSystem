using UnityEditor;
using UnityEngine;

namespace com.faolline.dialoguesystem
{
    [CustomPropertyDrawer(typeof(SpeakerExpression))]
    public class SpeakerExpressionDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var keyProp = property.FindPropertyRelative("key");
            var nameProp = property.FindPropertyRelative("displayName");
            var variantsProp = property.FindPropertyRelative("variants");

            float h = 0f;
            var line = EditorGUIUtility.singleLineHeight;

            // key + displayName on one row
            h += line + 4;

            // variants list (Unity calc avec nos drawers)
            h += EditorGUI.GetPropertyHeight(variantsProp, true) + 4;

            return h;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var keyProp = property.FindPropertyRelative("key");
            var nameProp = property.FindPropertyRelative("displayName");
            var variantsProp = property.FindPropertyRelative("variants");

            var line = EditorGUIUtility.singleLineHeight;
            float y = position.y;

            // Header row: key + displayName
            float half = (position.width - 6) * 0.5f;
            EditorGUI.PropertyField(new Rect(position.x, y, half, line), keyProp, new GUIContent("Key"));
            EditorGUI.PropertyField(new Rect(position.x + half + 6, y, half, line), nameProp, new GUIContent("Display"));
            y += line + 4;

            // Variants list
            EditorGUI.PropertyField(new Rect(position.x, y, position.width, EditorGUI.GetPropertyHeight(variantsProp, true)),
                                    variantsProp, new GUIContent("Variants"), true);

            EditorGUI.EndProperty();
        }
    }
}
