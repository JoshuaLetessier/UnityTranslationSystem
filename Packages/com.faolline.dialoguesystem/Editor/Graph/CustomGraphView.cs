using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace com.faolline.dialoguesystem
{

    public sealed class Flow { }

    public class CustomGraphView : GraphView
    {
        public Dialogue DialogueAsset { get; private set; }
        public System.Action<SentenceType, Vector2> onRequestCreateNode; // callback vers la fenêtre

        private readonly Dictionary<string, Node> _nodeViews = new();

        public System.Action<string> OnSelectionChanged;

        private HashSet<string> _currentSelection = new();

        public CustomGraphView()
        {
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            var grid = new GridBackground();
            grid.StretchToParentSize();
            Insert(0, grid);

            // Menu contextuel (clic droit)
            this.AddManipulator(new ContextualMenuManipulator(BuildContextMenu));

            graphViewChanged = OnGraphViewChanged;

            this.viewTransformChanged += OnViewTransformChanged;
        }

        public void Bind(Dialogue dialogue)
        {
            DialogueAsset = dialogue;
            _nodeViews.Clear();
        }

        // Menu contextuel
        private void BuildContextMenu(ContextualMenuPopulateEvent evt)
        {
            // Position curseur → coordonnées du content container
            var mouseWorld = evt.mousePosition; // en espace fenêtre
            var graphPos = contentViewContainer.WorldToLocal(mouseWorld);

            evt.menu.AppendAction("Add/Start", _ => onRequestCreateNode?.Invoke(SentenceType.Start, graphPos),
                                   _ => CanAddStart() ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            evt.menu.AppendAction("Add/Statement", _ => onRequestCreateNode?.Invoke(SentenceType.Statement, graphPos));
            evt.menu.AppendAction("Add/Choice", _ => onRequestCreateNode?.Invoke(SentenceType.Choice, graphPos));
            evt.menu.AppendAction("Add/End", _ => onRequestCreateNode?.Invoke(SentenceType.End, graphPos));
        }

        private bool CanAddStart() =>
            DialogueAsset == null || !DialogueAsset.Graph.nodes.Any(n => n.type == SentenceType.Start);


        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter _)
        {
            return ports
                .Where(p =>
                    p != startPort &&
                    p.direction != startPort.direction &&
                    p.portType == startPort.portType   // même type = Flow
                )
                .ToList();
        }

        // Wrapper création de ports (inchangé)
        public Port CreatePort(Node node, string portName, Direction dir, Port.Capacity cap, PortTag tag)
        {
            var port = node.InstantiatePort(Orientation.Horizontal, dir, cap, typeof(Flow));
            port.portName = portName;
            port.name = $"{tag.nodeId}.{tag.role}" + (tag.optionId != null ? $".{tag.optionId}" : "");
            port.userData = tag;

            if (tag.role == "Next") port.AddToClassList("ds-port-next");
            if (tag.role == "In") port.AddToClassList("ds-port-in");
            if (tag.role == "Option") port.AddToClassList("ds-port-option");
            if (tag.role == "True" || tag.role == "False")
                port.AddToClassList($"ds-port-{tag.role.ToLower()}");
            return port;
        }

        public void RegisterNodeView(string nodeId, Node view) => _nodeViews[nodeId] = view;
        public Node GetNodeView(string nodeId) => _nodeViews.TryGetValue(nodeId, out var v) ? v : null;

        public void BuildEdgesFromModel()
        {
            if (DialogueAsset == null) return;
            DeleteElements(edges.ToList());

            foreach (var e in DialogueAsset.Graph.edges)
            {
                var fromView = GetNodeView(e.fromNodeId.ToString()) as Node;
                var toView = GetNodeView(e.toNodeId.ToString()) as Node;
                if (fromView == null || toView == null) continue;

                var outPort = fromView.outputContainer.Children()
                    .OfType<Port>()
                    .FirstOrDefault(p =>
                    {
                        var tag = (PortTag)p.userData;
                        if (tag.nodeId != e.fromNodeId) return false;
                        if (e.portName.StartsWith("Option:"))
                        {
                            var optId = e.portName.Substring("Option:".Length);
                            return tag.role == "Option" && tag.optionId == optId;
                        }
                        return tag.role == e.portName;
                    });

                var inPort = toView.inputContainer.Children()
                    .OfType<Port>()
                    .FirstOrDefault(p => ((PortTag)p.userData).role == "In");

                if (outPort == null || inPort == null) continue;

                var edge = outPort.ConnectTo(inPort);
                AddElement(edge);
            }
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (DialogueAsset == null) return change;

            if (change.edgesToCreate != null)
            {
                foreach (var edge in change.edgesToCreate)
                {
                    var from = (PortTag)edge.output.userData;
                    var to = (PortTag)edge.input.userData;

                    Undo.RecordObject(DialogueAsset, "Connect Dialogue Edge");

                    if (edge.output.capacity == Port.Capacity.Single)
                    {
                        if (from.role == "Option")
                        {
                            // Ne supprime que l’edge de CETTE option, pas toutes les options
                            var thisOption = $"Option:{from.optionId}";
                            DialogueAsset.Graph.edges.RemoveAll(em =>
                                em.fromNodeId == from.nodeId &&
                                em.portName == thisOption
                            );
                        }
                        else
                        {
                            // Ex: "Next", "True", "False"
                            DialogueAsset.Graph.edges.RemoveAll(em =>
                                em.fromNodeId == from.nodeId &&
                                em.portName == from.role
                            );
                        }
                    }


                    var portName = from.role == "Option" ? $"Option:{from.optionId}" : from.role;

                    DialogueAsset.Graph.edges.Add(new DialogueEdgeModel
                    {
                        fromNodeId = from.nodeId,
                        toNodeId = to.nodeId,
                        portName = portName
                    });

                    EditorUtility.SetDirty(DialogueAsset);
                }
            }

            if (change.elementsToRemove != null)
            {
                foreach (var elem in change.elementsToRemove)
                {
                    if (elem is Node node && node.userData is string nodeId)
                    {
                        Undo.RecordObject(DialogueAsset, "Delete Dialogue Node");
                        // Retire le node du modèle
                        DialogueAsset.Graph.nodes.RemoveAll(n => n.id == nodeId);
                        // Et toutes ses edges (source ou cible)
                        DialogueAsset.Graph.edges.RemoveAll(e => e.fromNodeId == nodeId || e.toNodeId == nodeId);
                        EditorUtility.SetDirty(DialogueAsset);
                    }
                    else if (elem is Edge e)
                    {
                        var from = (PortTag)e.output.userData;
                        var to = (PortTag)e.input.userData;
                        Undo.RecordObject(DialogueAsset, "Disconnect Dialogue Edge");
                        var portName = from.role == "Option" ? $"Option:{from.optionId}" : from.role;
                        DialogueAsset.Graph.edges.RemoveAll(em =>
                            em.fromNodeId == from.nodeId &&
                            em.toNodeId == to.nodeId &&
                            em.portName == portName
                        );
                        EditorUtility.SetDirty(DialogueAsset);
                    }
                }
            }

            if (change.movedElements != null && change.movedElements.Count > 0)
            {
                Undo.RecordObject(DialogueAsset, "Move Dialogue Node");
                foreach (var el in change.movedElements)
                {
                    if (el is Node moved && moved.userData is string nodeId)
                    {
                        var rect = moved.GetPosition();
                        var model = DialogueAsset.Graph.nodes.FirstOrDefault(n => n.id == nodeId);
                        if (model != null) { model.position = rect.position; model.size = rect.size; }
                    }
                }
                EditorUtility.SetDirty(DialogueAsset);
            }

            return change;
        }

        private void OnViewTransformChanged(GraphView gv)
        {
            if (DialogueAsset == null) return;
            Undo.RecordObject(DialogueAsset, "Pan/Zoom Dialogue Graph");
            DialogueAsset.Graph.viewPosition = viewTransform.position;
            DialogueAsset.Graph.viewScale = viewTransform.scale.x; // uniforme
            EditorUtility.SetDirty(DialogueAsset);
        }


        public void NotifyNodeSelected(string nodeId)
        {
            _currentSelection.Add(nodeId);
            FireSelectionChanged();
        }

        public void NotifyNodeUnselected(string nodeId)
        {
            _currentSelection.Remove(nodeId);
            FireSelectionChanged();
        }

        private void FireSelectionChanged()
        {
            OnSelectionChanged?.Invoke(_currentSelection.Count == 1 ? _currentSelection.First() : string.Empty);
        }
    }
}
