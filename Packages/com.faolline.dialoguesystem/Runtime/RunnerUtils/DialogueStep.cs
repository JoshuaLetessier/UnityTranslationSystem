using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.faolline.dialoguesystem
{
    public abstract class DialogueStep { public readonly string nodeId; protected DialogueStep(string nodeId) { this.nodeId = nodeId; } }

    public sealed class LineStep : DialogueStep
    {
        public readonly string speakerKey;
        public readonly string expressionKey;
        public readonly string textKey;
        public LineStep(string nodeId, string speakerKey, string expressionKey, string textKey)
     : base(nodeId)
        {
            this.speakerKey = speakerKey ?? string.Empty;
            this.expressionKey = expressionKey ?? string.Empty;
            this.textKey = textKey ?? string.Empty;
        }
    }

    public sealed class ChoicesStep : DialogueStep
    {
        public sealed class Item
        {
            public readonly string optionId;
            public readonly int index;        // 1..N (ordre d’affichage)
            public readonly string textKey;
            public readonly bool allowed;     // conditions option respectées
            public Item(string optionId, int index, string textKey, bool allowed)
            { this.optionId = optionId; this.index = index; this.textKey = textKey ?? string.Empty; this.allowed = allowed; }
        }

        public readonly List<Item> items;
        public ChoicesStep(string nodeId, List<Item> items) : base(nodeId) { this.items = items ?? new List<Item>(); }
    }

    public sealed class EndStep : DialogueStep
    {
        public readonly EndReason reason;
        public readonly string error;
        public EndStep(string nodeId, EndReason reason, string error = null) : base(nodeId)
        { this.reason = reason; this.error = error ?? string.Empty; }
    }
}
