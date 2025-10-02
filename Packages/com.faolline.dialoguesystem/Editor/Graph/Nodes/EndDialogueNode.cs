using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace com.faolline.dialoguesystem
{
    public class EndDialogueNode : DialogueNode
    {
        private readonly CustomGraphView _graph;
        private readonly SentenceNodeModel _model;

        public EndDialogueNode(CustomGraphView graph, SentenceNodeModel model) : base(graph, model)
        {

            _graph = graph;
            _model = model; 

            title = "End";

            BindFromDTO(_model.position, _model.size);

            InitNode();
        }

        protected override void InitNode()
        {
            // Input "End"
            var inPort = _graph.CreatePort(this, "In", Direction.Input, Port.Capacity.Multi,
               new PortTag(_model.id, "In"));
            inputContainer.Add(inPort);

            RefreshExpandedState();
            RefreshPorts();
            MarkDirtyRepaint();
        }
    }
}
