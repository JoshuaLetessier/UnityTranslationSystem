using UnityEditor;
using UnityEngine;

namespace com.faolline.translationsystem
{
    public static class TextRichEditor
    {
        private class InsertionState
        {
            public string StartTag;
            public string EndTag;
            public bool AwaitingSecondClick;
        }

        private static InsertionState currentState = null;

        private static bool pendingInsertionRequest = false;
        private static bool waitOneFrameAfterFocus = false;


        public static void DrawToolbar(System.Action<string> onUpdate)
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);


            if (GUILayout.Button("𝐁", EditorStyles.toolbarButton))
                BeginTagInsertion("<b>", "</b>");

            if (GUILayout.Button("𝑰", EditorStyles.toolbarButton))
                BeginTagInsertion("<i>", "</i>");

            if (GUILayout.Button("̲U̲", EditorStyles.toolbarButton))
                BeginTagInsertion("<u>", "</u>");

            if (GUILayout.Button("S̶", EditorStyles.toolbarButton))
                BeginTagInsertion("<s>", "</s>");

            if (currentState != null)
            {
                if (GUILayout.Button("✅ Insérer balise ici", EditorStyles.toolbarButton))
                {
                    pendingInsertionRequest = true;
                    waitOneFrameAfterFocus = true; // laisse passer une frame pour que le cursorIndex se mette à jour
                }


                if (GUILayout.Button("❌ Annuler", EditorStyles.toolbarButton))
                    currentState = null;
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }


        private static void BeginTagInsertion(string startTag, string endTag)
        {
            currentState = new InsertionState
            {
                StartTag = startTag,
                EndTag = endTag,
                AwaitingSecondClick = false
            };
        }

        private static void DrawStatusMessage()
        {
            if (currentState == null) return;
            string message = currentState.AwaitingSecondClick
                ? $"🟦 Cliquez dans le champ pour insérer la balise de fin : {currentState.EndTag}"
                : $"🟨 Cliquez dans le champ pour insérer la balise de début : {currentState.StartTag}";

            EditorGUILayout.HelpBox(message, MessageType.Info);
        }

        public static void HandleTagInsertion(ref string text)
        {
            if (currentState == null)
                return;

            if (waitOneFrameAfterFocus)
            {
                waitOneFrameAfterFocus = false;
                return; // Laisse passer une frame
            }

            if (!pendingInsertionRequest)
                return;

            var editor = GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl) as TextEditor;
            if (editor == null)
                return;

            int index = editor.cursorIndex;

            Debug.Log(index);

            if (index < 0 || index > text.Length) index = text.Length;

            if (!currentState.AwaitingSecondClick)
            {
                text = text.Insert(index, currentState.StartTag);
                editor.cursorIndex = editor.selectIndex = index + currentState.StartTag.Length;
                currentState.AwaitingSecondClick = true;
            }
            else
            {
                text = text.Insert(index, currentState.EndTag);
                editor.cursorIndex = editor.selectIndex = index + currentState.EndTag.Length;
                currentState = null;
            }

            pendingInsertionRequest = false;
        }


    }
}
