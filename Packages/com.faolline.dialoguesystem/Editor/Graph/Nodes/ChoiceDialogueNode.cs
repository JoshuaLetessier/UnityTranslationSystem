using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements; // << nécessaire pour Button & ContextualMenu

namespace com.faolline.dialoguesystem
{
    /// <summary>
    /// NodeView pour un Choice : 1 input "In" (Multi) / N outputs "Option i" (Single)
    /// Chaque port d’option est taggé avec optionId (GUID stable).
    /// </summary>
    public class ChoiceDialogueNode : DialogueNode
    {
        private readonly CustomGraphView _graph;
        private readonly SentenceNodeModel _model;

        // Toolbar locale (+ / -)
        private VisualElement _choiceToolbar;
        private Button _btnAdd;
        private Button _btnRemove;

        public ChoiceDialogueNode(CustomGraphView graph, SentenceNodeModel model) : base(graph, model)
        {
            _graph = graph;
            _model = model;

            if (_model.options == null) _model.options = new List<ChoiceOptionModel>();

            title = "CHOICE";

            // Bind position/size depuis le modèle (source of truth)
            if (_model.size == Vector2.zero) _model.size = new Vector2(260, 140);
            BindFromDTO(_model.position, _model.size);

            InitNode();
        }

        ~ChoiceDialogueNode()
        {
            DialogueNode.OnNodeDataChanged -= HandleExternalChoiceDataChanged;
        }

        protected override void InitNode()
        {
            // INPUT "In" (Multi)
            var inPort = _graph.CreatePort(this, "In", Direction.Input, Port.Capacity.Multi,
                new PortTag(_model.id, "In"));
            inputContainer.Add(inPort);

            // Toolbar +/-
            EnsureToolbar();

            // Sorties pour chaque option existante (si vide, on n'en crée pas)
            RebuildOptionPorts();

            RefreshExpandedState();
            RefreshPorts();
            MarkDirtyRepaint();

            DialogueNode.OnNodeDataChanged += HandleExternalChoiceDataChanged;
        }

        private void EnsureToolbar()
        {
            if (_choiceToolbar != null) return;

            _choiceToolbar = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    marginLeft = 4,
                    marginRight = 4,
                    marginBottom = 2,
                    justifyContent = Justify.FlexEnd,
                }
            };

            _btnAdd = new Button(() => AddChoice()) { text = "+", tooltip = "Add Choice" };
            _btnAdd.style.width = 22; _btnAdd.style.height = 16;

            _btnRemove = new Button(() => RemoveLastChoice()) { text = "–", tooltip = "Remove last Choice" };
            _btnRemove.style.width = 22; _btnRemove.style.height = 16;

            // Place la toolbar sous le titre, avant les ports
            titleContainer.Add(_choiceToolbar);
            _choiceToolbar.Add(_btnAdd);
            _choiceToolbar.Add(_btnRemove);
        }

        /// <summary>Reconstruit uniquement les ports d’option depuis _model.options.</summary>
        private void RebuildOptionPorts()
        {
            // Supprimer uniquement les ports "Option"
            foreach (var p in outputContainer.Children().OfType<Port>().ToList())
            {
                var tag = (PortTag)p.userData;
                if (tag.role == "Option")
                    outputContainer.Remove(p);
            }

            int idx = 0;
            foreach (var opt in _model.options)
            {
                var port = _graph.CreatePort(this, $"Option {++idx}", Direction.Output, Port.Capacity.Single,
                    new PortTag(_model.id, "Option", opt.optionId));

                // Menu contextuel pour suppression ciblée
                port.AddManipulator(new ContextualMenuManipulator(evt =>
                {
                    evt.menu.AppendAction("Remove Choice",
                        _ => RemoveChoice(opt.optionId),
                        _ => DropdownMenuAction.Status.Normal);
                }));

                outputContainer.Add(port);
            }

            RefreshPorts();
            RefreshExpandedState();
            MarkDirtyRepaint();
        }

        /// <summary>Ajoute une option au modèle + crée le port correspondant.</summary>
        public void AddChoice()
        {
            Undo.RecordObject(_graph.DialogueAsset, "Add Choice Option");

            var opt = new ChoiceOptionModel
            {
                optionId = Guid.NewGuid().ToString(),
                displayTextKey = string.Empty,
                conditions = Array.Empty<DialogueCondition>(),
                sideEffects = Array.Empty<DialogueAction>()
            };
            _model.options.Add(opt);
            EditorUtility.SetDirty(_graph.DialogueAsset);

            // Crée le port taggé avec optionId
            var port = _graph.CreatePort(this, $"Option {_model.options.Count}", Direction.Output, Port.Capacity.Single,
                new PortTag(_model.id, "Option", opt.optionId));

            port.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                evt.menu.AppendAction("Remove Choice",
                    _ => RemoveChoice(opt.optionId),
                    _ => DropdownMenuAction.Status.Normal);
            }));

            outputContainer.Add(port);

            RefreshPorts();
            RefreshExpandedState();
            MarkDirtyRepaint();
        }

        /// <summary>Supprime la dernière option (fallback pour le bouton "–").</summary>
        private void RemoveLastChoice()
        {
            if (_model.options == null || _model.options.Count == 2) return;
            var last = _model.options[_model.options.Count - 1];
            RemoveChoice(last.optionId);
        }

        /// <summary>Supprime une option par optionId (modèle, ports, edges).</summary>
        public void RemoveChoice(string optionId)
        {
            var idx = _model.options.FindIndex(o => o.optionId == optionId);
            if (idx < 0) return;

            Undo.RecordObject(_graph.DialogueAsset, "Remove Choice Option");

            // 1) Supprimer toutes les edges du modèle liées à cette option
            _graph.DialogueAsset.Graph.edges.RemoveAll(e =>
                e.fromNodeId == _model.id &&
                e.portName == $"Option:{optionId}"
            );

            // 2) Supprimer l’option du modèle
            _model.options.RemoveAt(idx);
            EditorUtility.SetDirty(_graph.DialogueAsset);

            // 3) Supprimer le port correspondant (et edges visuels)
            var toRemove = outputContainer.Children()
                .OfType<Port>()
                .FirstOrDefault(p =>
                {
                    var t = (PortTag)p.userData;
                    return t.role == "Option" && t.optionId == optionId;
                });

            if (toRemove != null)
            {
                foreach (var e in toRemove.connections?.ToList() ?? Enumerable.Empty<Edge>())
                    _graph.DeleteElements(new[] { e });

                outputContainer.Remove(toRemove);
            }

            // 4) Renommer l’affichage des ports "Option i"
            int display = 0;
            foreach (var p in outputContainer.Children().OfType<Port>())
            {
                var t = (PortTag)p.userData;
                if (t.role == "Option")
                    p.portName = $"Option {++display}";
            }

            RefreshPorts();
            RefreshExpandedState();
            MarkDirtyRepaint();
        }

        /// <summary>Persistance pos/size vers le DTO (appelée par la fenêtre quand la géométrie change).</summary>
        public void WriteBack()
        {
            WriteBackToDTO(ref _model.position, ref _model.size);
        }

        private void HandleExternalChoiceDataChanged(string nodeId)
        {
            if (!string.Equals(nodeId, _model.id, StringComparison.Ordinal)) return;
            RefreshOptionsFromModel();
        }

        public void RefreshOptionsFromModel()
        {
            RebuildOptionPorts(); // rends cette méthode 'public' OU appelle ce wrapper
        }
    }
}
