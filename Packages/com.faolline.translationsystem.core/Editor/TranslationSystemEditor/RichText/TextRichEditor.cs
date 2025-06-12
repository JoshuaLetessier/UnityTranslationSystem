using UnityEditor;
using UnityEngine;
using System;

namespace com.faolline.translationsystem
{
    public static class TextRichEditor
    {
        public static void DrawToolbar(System.Action<string> onUpdate)
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("🔠 UPPERCASE", EditorStyles.toolbarButton))
            {
                onUpdate?.Invoke("TO_BE_UPPERCASED"); // à remplacer par une vraie logique
            }

            if (GUILayout.Button("🔡 lowercase", EditorStyles.toolbarButton))
            {
                onUpdate?.Invoke("to_be_lowercased"); // pareil
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
    }

}
