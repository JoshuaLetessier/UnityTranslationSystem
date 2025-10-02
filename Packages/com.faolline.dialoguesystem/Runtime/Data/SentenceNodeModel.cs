using System;
using System.Collections.Generic;
using UnityEngine;


namespace com.faolline.dialoguesystem
{
    [Serializable]
    public class SentenceNodeModel
    {
        public string id;
        [SerializeReference] public SentenceType type;
        public string speakerKey;
        public string textKey;

        public string speakerExpressionKey = "neutral";

        public Vector2 position;
        public Vector2 size;
        [SerializeReference] public List<ChoiceOptionModel> options;

        public List<DialogueCondition> conditions = new();
        public List<DialogueAction> actions = new();
    }
}
