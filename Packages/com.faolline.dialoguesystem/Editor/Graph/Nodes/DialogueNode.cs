using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace com.faolline.dialoguesystem
{
    public enum IssueSeverity { Info, Warning, Error }

    [Serializable]
    public struct NodeIssue
    {
        public IssueSeverity severity;
        public string message;
        public string field; // ex: "TextKey", "TargetNodeId"
    }

    /// <summary>
    /// Base abstraite pour les NodeView (éditeur).
    /// La "source of truth" pour pos/size est le DTO : on lit/écrit via Bind/WriteBack.
    /// Les états visuels se font via classes USS.
    /// </summary>
    public abstract class DialogueNode : Node
    {
        protected readonly CustomGraphView graph;
        protected readonly SentenceNodeModel model;

        protected Vector2 _position;
        protected Vector2 _size = new Vector2(200, 150);
        protected bool expandedState = false;

        // États visuels via classes USS
        const string kClsSelected = "ds-selected";
        const string kClsHighlight = "ds-highlight";
        const string kClsWarning = "ds-warning";
        const string kClsError = "ds-error";
        const string kClsLocked = "ds-locked";

        // Issues affichées
        protected readonly List<NodeIssue> _issues = new();

        // ── Badges (overlay)
        private VisualElement _badgeContainer;
        private Label _condBadge;
        private Label _actBadge;

        // ── Bus d’événement (inspector → nodes)
        public static event Action<string> OnNodeDataChanged; // payload: nodeId
        public static void BroadcastNodeDataChanged(string nodeId) => OnNodeDataChanged?.Invoke(nodeId);

        protected DialogueNode(CustomGraphView graph, SentenceNodeModel model)
        {
            this.graph = graph;
            this.model = model ?? throw new ArgumentNullException(nameof(model));

            // Chrome commun
            capabilities |= Capabilities.Movable | Capabilities.Deletable | Capabilities.Selectable |
                           Capabilities.Ascendable | Capabilities.Collapsible;

            // Taille/position par défaut
            base.SetPosition(new Rect(_position, _size));

            // Hook geometry
            RegisterCallback<GeometryChangedEvent>(_ => OnGeometryChanged());

            // Abonnement bus : si un autre composant signale une modif sur ce node, on refresh les badges
            OnNodeDataChanged += HandleExternalDataChange;
        }

        /// <summary>À implémenter : création ports, layout, éléments d’UI.</summary>
        protected abstract void InitNode();

        /// <summary>Binding depuis le DTO (position, size, data). Appelle ensuite ApplyVisuals().</summary>
        public void BindFromDTO(Vector2 pos, Vector2 size)
        {
            _position = pos;
            _size = size == Vector2.zero ? new Vector2(200, 150) : size;
            base.SetPosition(new Rect(_position, _size));
            ApplyVisuals();
        }

        /// <summary>Écrit la position/taille actuelles dans ton DTO (à appeler côté fenêtre/graph).</summary>
        public void WriteBackToDTO(ref Vector2 pos, ref Vector2 size)
        {
            pos = _position;
            size = _size;
        }

        // ────────────────────────────────────────────────────────────────────────
        // Visuels
        // ────────────────────────────────────────────────────────────────────────

        /// <summary>Applique couleur, badges, états.</summary>
        protected virtual void ApplyVisuals()
        {
            // Nettoyage classes warning/error
            RemoveFromClassList(kClsError);
            RemoveFromClassList(kClsWarning);

            if (_issues.Count > 0)
            {
                var max = IssueSeverity.Info;
                foreach (var i in _issues) if (i.severity > max) max = i.severity;
                if (max == IssueSeverity.Error) AddToClassList(kClsError);
                else if (max == IssueSeverity.Warning) AddToClassList(kClsWarning);
            }

            EnsureBadges();
            RefreshBadges();

            RefreshExpandedState();
            RefreshPorts();
            MarkDirtyRepaint();
        }

        public void SetHighlight(bool on)
        {
            if (on) AddToClassList(kClsHighlight);
            else RemoveFromClassList(kClsHighlight);
        }

        public void SetLocked(bool locked)
        {
            if (locked)
            {
                AddToClassList(kClsLocked);
                capabilities &= ~Capabilities.Movable;
                capabilities &= ~Capabilities.Deletable;
            }
            else
            {
                RemoveFromClassList(kClsLocked);
                capabilities |= Capabilities.Movable;
                capabilities |= Capabilities.Deletable;
            }
        }

        public void SetIssues(IEnumerable<NodeIssue> issues)
        {
            _issues.Clear();
            if (issues != null) _issues.AddRange(issues);
            ApplyVisuals();
        }

        // ── Sélection
        public override void OnSelected()
        {
            base.OnSelected();
            AddToClassList(kClsSelected);
            graph?.NotifyNodeSelected(model.id);
        }

        public override void OnUnselected()
        {
            base.OnUnselected();
            RemoveFromClassList(kClsSelected);
            graph?.NotifyNodeUnselected(model.id);
        }

        // ── Position / taille
        public override void SetPosition(Rect newPos)
        {
            _position = newPos.position;
            _size = newPos.size;
            base.SetPosition(newPos);
        }

        public void SetPositionFrom(Vector2 pos, Vector2 size)
        {
            _position = pos;
            _size = size;
            base.SetPosition(new Rect(_position, _size));
        }

        void OnGeometryChanged()
        {
            var r = GetPosition();
            _position = r.position;
            _size = r.size;
            // ici ta fenêtre peut faire Undo.Record + WriteBack vers DTO si besoin
        }

        // ────────────────────────────────────────────────────────────────────────
        // Badges overlay
        // ────────────────────────────────────────────────────────────────────────

        private void EnsureBadges()
        {
            if (_badgeContainer != null) return;

            // Conteneur overlay top-right
            _badgeContainer = new VisualElement();
            _badgeContainer.style.position = Position.Absolute;
            _badgeContainer.style.top = -20;
            _badgeContainer.style.right = 6;
            _badgeContainer.pickingMode = PickingMode.Ignore; // laisse passer les events souris
            _badgeContainer.style.flexDirection = FlexDirection.Row;

            // Pastille Conditions
            _condBadge = new Label();
            StyleBadge(_condBadge);
            _condBadge.AddToClassList("ds-badge-cond");

            // Pastille Actions
            _actBadge = new Label();
            StyleBadge(_actBadge);
            _actBadge.AddToClassList("ds-badge-act");

            _badgeContainer.Add(_condBadge);
            _badgeContainer.Add(_actBadge);

            // On l’accroche sur le titleContainer pour qu’il reste en haut
            this.Add(_badgeContainer);
        }

        private static void StyleBadge(Label b)
        {
            b.style.paddingLeft = 6;
            b.style.paddingRight = 6;
            b.style.height = 16;
            b.style.unityTextAlign = TextAnchor.MiddleCenter;
            b.style.borderTopLeftRadius = 8;
            b.style.borderTopRightRadius = 8;
            b.style.borderBottomLeftRadius = 8;
            b.style.borderBottomRightRadius = 8;
            b.style.fontSize = 10;
            b.style.opacity = 0.95f;

            // Couleurs soft (adaptables via USS si tu préfères)
            b.style.backgroundColor = new Color(0.20f, 0.24f, 0.32f, 1f);
            b.style.color = Color.white;
        }

        private string BuildTypesTooltip<T>(IReadOnlyList<T> list) where T : UnityEngine.Object
        {
            if (list == null || list.Count == 0) return string.Empty;
            var names = list.Where(x => x != null).Select(x => x.GetType().Name).ToList();
            if (names.Count <= 3) return string.Join(", ", names);
            return string.Join(", ", names.Take(3)) + $" … (+{names.Count - 3})";
        }

        protected void RefreshBadges()
        {
            if (_badgeContainer == null) return;

            int c = model?.conditions?.Count ?? 0;
            int a = model?.actions?.Count ?? 0;

            // Conditions
            if (c > 0)
            {
                _condBadge.text = $"C {c}";
                _condBadge.style.display = DisplayStyle.Flex;
                _condBadge.tooltip = BuildTypesTooltip(model.conditions);
            }
            else
            {
                _condBadge.style.display = DisplayStyle.None;
                _condBadge.tooltip = string.Empty;
            }

            // Actions
            if (a > 0)
            {
                _actBadge.text = $"A {a}";
                _actBadge.style.display = DisplayStyle.Flex;
                _actBadge.tooltip = BuildTypesTooltip(model.actions);
            }
            else
            {
                _actBadge.style.display = DisplayStyle.None;
                _actBadge.tooltip = string.Empty;
            }
        }

        private void HandleExternalDataChange(string nodeId)
        {
            if (!string.Equals(nodeId, model.id, StringComparison.Ordinal)) return;
            // Seul ce node est concerné → refresh léger
            EnsureBadges();
            RefreshBadges();
            MarkDirtyRepaint();
        }

        // ── Expanded/Collapsed
        public void SetExpanded(bool expanded)
        {
            if (expanded == expandedState) return;
            expandedState = expanded;
            RefreshExpandedState();
        }
        public bool IsExpanded() => expandedState;

        // Get
        public SentenceNodeModel sentenceNodeModel => model;
    }
}
