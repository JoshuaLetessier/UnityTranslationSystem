using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace com.faolline.dialoguesystem
{
    /// <summary>
    /// NodeView pour un Statement : 1 input "In" (Multi) / 1 output "Next" (Single)
    /// </summary>
    public class StatementDialogueNode : DialogueNode
    {
        private readonly CustomGraphView _graph;
        private readonly SentenceNodeModel _model;

        public StatementDialogueNode(CustomGraphView graph, SentenceNodeModel model) : base(graph, model)
        {
            _graph = graph;
            _model = model;

            title = "STATEMENT";
            // Bind position/size depuis le modèle (source of truth)
            BindFromDTO(_model.position, _model.size);

            InitNode();
        }

        protected override void InitNode()
        {
            // INPUT "In" (Multi)
            var inPort = _graph.CreatePort(this, "In", Direction.Input, Port.Capacity.Multi,
                new PortTag(_model.id, "In"));
            inputContainer.Add(inPort);

            // OUTPUT "Next" (Single)
            var nextPort = _graph.CreatePort(this, "Next", Direction.Output, Port.Capacity.Single,
                new PortTag(_model.id, "Next"));
            outputContainer.Add(nextPort);

            RefreshExpandedState();
            RefreshPorts();
            MarkDirtyRepaint();
        }

        // Appelé par la fenêtre après déplacement/redimensionnement pour persister dans le DTO
        public void WriteBack()
        {
            WriteBackToDTO(ref _model.position, ref _model.size);
        }
    }
}
