using UnityEditor;
using UnityEngine;
using System;

namespace com.faolline.translationsystem
{
    public class TextRichEditor
    {
        //private const string TextControlName = "RichTextEditorArea";
        //private static bool shouldFocusTextArea = false; // Changed to static

        //public static void DrawToolbar(Action<string> onTextModified)
        //{
        //    GUILayout.BeginHorizontal(EditorStyles.helpBox);

        //    if (GUILayout.Button("B", GUILayout.Width(25))) InsertTag("b", onTextModified);
        //    if (GUILayout.Button("I", GUILayout.Width(25))) InsertTag("i", onTextModified);
        //    if (GUILayout.Button("U", GUILayout.Width(25))) InsertTag("u", onTextModified);
        //    if (GUILayout.Button("Red", GUILayout.Width(40))) InsertTag("color=red", onTextModified);
        //    if (GUILayout.Button("Big", GUILayout.Width(40))) InsertTag("size=24", onTextModified);

        //    GUILayout.EndHorizontal();
        //}

        //private static void InsertTag(string tag, Action<string> onTextModified)
        //{
        //    EditorApplication.delayCall += () =>
        //    {
        //        if (GUI.GetNameOfFocusedControl() != TextControlName)
        //        {
        //            Debug.Log("Control not focused");
        //            return;
        //        }

        //        if (!EditorGUIUtility.editingTextField) return;

        //        TextEditor editor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
        //        if (editor == null) return;
        //        Debug.Log($"Cursor: {editor.cursorIndex}, Select: {editor.selectIndex}, Text: {editor.text}");

        //        int start = Mathf.Min(editor.cursorIndex, editor.selectIndex);
        //        int end = Mathf.Max(editor.cursorIndex, editor.selectIndex);

        //        string currentText = GUI.GetNameOfFocusedControl() == TextControlName ? editor.text : null;
        //        if (string.IsNullOrEmpty(currentText)) return;

        //        if (start < 0 || end > currentText.Length || start == end) return;

        //        string selected = currentText.Substring(start, end - start);
        //        string open = $"<{tag}>";
        //        string close = $"</{tag.Split('=')[0]}>";

        //        string newText = currentText.Substring(0, start) + open + selected + close + currentText.Substring(end);
        //        onTextModified?.Invoke(newText);
        //    };
        //}

        //public static string DrawEditableArea(string content, ref bool shouldFocus)
        //{
        //    GUI.SetNextControlName(TextControlName);
        //    string result = EditorGUILayout.TextArea(content, GUILayout.ExpandHeight(true), GUILayout.MinHeight(120));

        //    if (shouldFocus && Event.current.type == EventType.Repaint)
        //    {
        //        GUI.FocusControl(TextControlName);
        //        shouldFocus = false;
        //    }

        //    return result;
        //}

    }
}
