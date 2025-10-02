using System;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace com.faolline.dialoguesystem
{
    public class StartDialogueNode : DialogueNode
    {
        private readonly CustomGraphView _graph;
        private readonly SentenceNodeModel _model;

        public StartDialogueNode(CustomGraphView graph, SentenceNodeModel model) : base(graph, model)
        {
            _graph = graph;
            _model = model;

            title = "START";

            capabilities &= ~(Capabilities.Deletable | Capabilities.Copiable);

            InitNode();
        }

        protected override void InitNode()
        {
            // Sortie "Next"
            var nextPort = _graph.CreatePort(this, "Next", Direction.Output, Port.Capacity.Single,
                            new PortTag(_model.id, "Next"));
            outputContainer.Add(nextPort);

            RefreshExpandedState();
            RefreshPorts();
            MarkDirtyRepaint();
        }
    }
}
