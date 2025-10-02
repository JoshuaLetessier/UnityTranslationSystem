using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace com.faolline.dialoguesystem
{
    // ---- États & fin ----
    public enum RunnerState { Idle, LineReady, ChoiceReady, Ended }
    public enum EndReason { EndNode, NoEdges, ConditionsFailed, GraphError, Aborted }

    // ---- Steps ----
    public abstract class DialogueStep { public readonly string nodeId; protected DialogueStep(string nodeId) { this.nodeId = nodeId; } }

    public sealed class LineStep : DialogueStep
    {
        public readonly string speakerId;
        public readonly Mood speakerMood;
        public readonly string textKey;
        public LineStep(string nodeId, string speakerId, Mood mood, string textKey) : base(nodeId)
        { this.speakerId = speakerId ?? string.Empty; this.speakerMood = mood; this.textKey = textKey ?? string.Empty; }
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


    // ---- Runner headless ----
    public sealed class DialogueRunner
    {
        public Dialogue Dialogue { get; }
        public IContext Context { get; }

        public RunnerState State { get; private set; } = RunnerState.Idle;
        public string CurrentNodeId { get; private set; }
        public DialogueStep CurrentStep { get; private set; }
        public string LastError { get; private set; }

        // (optionnels) callbacks d’observation
        public event Action<string> OnNodeChanged;
        public event Action<LineStep> OnLine;
        public event Action<ChoicesStep> OnChoices;
        public event Action<EndStep> OnEnd;

        // caches graphe
        private Dictionary<string, SentenceNodeModel> _nodeById;
        private Dictionary<string, List<DialogueEdgeModel>> _edgesFrom;
        private SentenceNodeModel _startNode;

        // mémoire des choix proposés lors de Proceed() sur un Choice
        private List<ChoicesStep.Item> _lastChoiceItems;

        // reflection signatures
        static readonly Type[] _sigEvalCtx = { typeof(IContext) };
        static readonly Type[] _sigEvalNo = Type.EmptyTypes;
        static readonly Type[] _sigExecCtx = { typeof(IContext) };
        static readonly Type[] _sigExecNo = Type.EmptyTypes;

        public DialogueRunner(Dialogue dialogue, IContext context = null)
        {
            Dialogue = dialogue ?? throw new ArgumentNullException(nameof(dialogue));
            Context = context ?? DefaultContext.Instance;
            BuildGraphCaches();
        }

        // ───────────────────────────────────────────────────────────────────────
        // API
        // ───────────────────────────────────────────────────────────────────────

        /// <summary>Démarre au node Start : vérifie condition du node, émet sa Line ou termine.</summary>
        public DialogueStep Start()
        {
            EnsureIdle();
            if (_startNode == null) return EndNow(EndReason.GraphError, "Start node not found");
            return EnterNode(_startNode.id);
        }

        /// <summary>Ordre d’avancer depuis un Line (Start/Statement/Choice/End).</summary>
        public DialogueStep Proceed()
        {
            if (State != RunnerState.LineReady)
                return CurrentStep ?? EndNow(EndReason.GraphError, "Proceed() invalid state");

            var node = GetNode(CurrentNodeId);
            if (node == null) return EndNow(EndReason.GraphError, "Current node missing");

            switch (node.type)
            {
                case SentenceType.Start:
                    {
                        ExecuteActionsSafe(node.actions);
                        var e = GetUniqueOutgoing(node.id);
                        if (e == null) return EndNow(EndReason.NoEdges, "Start has no/invalid edge");
                        return EnterNode(e.toNodeId);
                    }

                case SentenceType.Statement:
                    {
                        ExecuteActionsSafe(node.actions);
                        var e = GetNextEdgeForStatement(node.id);
                        if (e == null) return EndNow(EndReason.NoEdges, "Statement Next edge missing");
                        return EnterNode(e.toNodeId);
                    }

                case SentenceType.Choice:
                    {
                        // Construire les items + état (allowed) ; ne pas naviguer tant qu’un choix n’est pas choisi.
                        _lastChoiceItems = BuildChoiceItems(node);
                        var step = new ChoicesStep(node.id, _lastChoiceItems);
                        State = RunnerState.ChoiceReady;
                        SetStep(step);
                        return step;
                    }

                case SentenceType.End:
                    {
                        ExecuteActionsSafe(node.actions); // actions de fin éventuelles
                        return EndNow(EndReason.EndNode);
                    }

                default:
                    return EndNow(EndReason.GraphError, $"Unsupported node type: {node.type}");
            }
        }

        /// <summary>Sélection d’un choix par index d’affichage (1..N).</summary>
        public DialogueStep ChooseByIndex(int index)
        {
            if (State != RunnerState.ChoiceReady || _lastChoiceItems == null)
                return CurrentStep ?? EndNow(EndReason.GraphError, "ChooseByIndex() invalid state");

            var item = _lastChoiceItems.FirstOrDefault(i => i.index == index);
            return item == null ? CurrentStep : ChooseById(item.optionId);
        }

        /// <summary>Sélection d’un choix par optionId (ne navigue que si allowed).</summary>
        public DialogueStep ChooseById(string optionId)
        {
            if (State != RunnerState.ChoiceReady || _lastChoiceItems == null)
                return CurrentStep ?? EndNow(EndReason.GraphError, "ChooseById() invalid state");

            var item = _lastChoiceItems.FirstOrDefault(i => i.optionId == optionId);
            if (item == null) return CurrentStep;

            var choiceNode = GetNode(CurrentNodeId);
            if (choiceNode == null) return EndNow(EndReason.GraphError, "Choice node missing");

            // Si l’option n’est pas autorisée, on n’avance pas (au choix : ignorer ou lever erreur)
            if (!item.allowed) return CurrentStep;

            // Effets : d’abord l’option, puis l’action globale du Choice
            var opt = GetChoiceOption(choiceNode, item.optionId);
            if (opt != null) ExecuteActionsSafe(opt.sideEffects);
            ExecuteActionsSafe(choiceNode.actions);

            var edge = GetOptionEdge(choiceNode.id, item.optionId);
            if (edge == null) return EndNow(EndReason.NoEdges, $"Missing edge for Option:{item.optionId}");

            _lastChoiceItems = null;
            return EnterNode(edge.toNodeId);
        }

        public DialogueStep Abort(string message = null) => EndNow(EndReason.Aborted, message ?? "Aborted");

        // ───────────────────────────────────────────────────────────────────────
        // Cœur
        // ───────────────────────────────────────────────────────────────────────

        private DialogueStep EnterNode(string nodeId)
        {
            var node = GetNode(nodeId);
            if (node == null) return EndNow(EndReason.GraphError, $"Node not found: {nodeId}");

            // Gate : conditions du node
            if (!EvaluateConditionsSafe(node.conditions))
                return EndNow(EndReason.ConditionsFailed, $"Node {node.id} conditions failed");

            CurrentNodeId = node.id;
            _lastChoiceItems = null;
            OnNodeChanged?.Invoke(CurrentNodeId);

            // Toujours émettre la "ligne de base" du node
            var step = new LineStep(node.id, node.speakerId, node.speakerMood, node.textKey);
            State = RunnerState.LineReady;
            SetStep(step);
            return step;
        }

        private void SetStep(DialogueStep step)
        {
            CurrentStep = step;
            switch (step)
            {
                case LineStep l: OnLine?.Invoke(l); break;
                case ChoicesStep c: OnChoices?.Invoke(c); break;
                case EndStep e: OnEnd?.Invoke(e); break;
            }
        }

        private DialogueStep EndNow(EndReason reason, string error = null)
        {
            State = RunnerState.Ended;
            LastError = error ?? string.Empty;
            var step = new EndStep(CurrentNodeId, reason, error);
            SetStep(step);
            return step;
        }

        // ───────────────────────────────────────────────────────────────────────
        // Graph utils
        // ───────────────────────────────────────────────────────────────────────

        private void BuildGraphCaches()
        {
            var g = Dialogue?.Graph;
            if (g == null) throw new InvalidOperationException("Dialogue has no Graph");

            _nodeById = (g.nodes ?? new List<SentenceNodeModel>()).ToDictionary(n => n.id, n => n);
            _edgesFrom = new Dictionary<string, List<DialogueEdgeModel>>();
            foreach (var e in g.edges ?? new List<DialogueEdgeModel>())
            {
                if (e == null || string.IsNullOrEmpty(e.fromNodeId)) continue;
                if (!_edgesFrom.TryGetValue(e.fromNodeId, out var list)) _edgesFrom[e.fromNodeId] = list = new List<DialogueEdgeModel>();
                list.Add(e);
            }
            _startNode = _nodeById.Values.FirstOrDefault(n => n.type == SentenceType.Start);
        }

        private SentenceNodeModel GetNode(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return _nodeById != null && _nodeById.TryGetValue(id, out var n) ? n : null;
        }

        private DialogueEdgeModel GetUniqueOutgoing(string fromNodeId)
        {
            if (!_edgesFrom.TryGetValue(fromNodeId, out var list) || list == null || list.Count != 1) return null;
            return list[0];
        }

        private DialogueEdgeModel GetNextEdgeForStatement(string fromNodeId)
        {
            if (!_edgesFrom.TryGetValue(fromNodeId, out var list) || list == null) return null;
            return list.FirstOrDefault(e => e.portName == "Next");
        }

        private DialogueEdgeModel GetOptionEdge(string fromNodeId, string optionId)
        {
            if (string.IsNullOrEmpty(optionId)) return null;
            if (!_edgesFrom.TryGetValue(fromNodeId, out var list) || list == null) return null;
            var pname = "Option:" + optionId;
            return list.FirstOrDefault(e => e.portName == pname);
        }

        private ChoiceOptionModel GetChoiceOption(SentenceNodeModel node, string optionId)
        {
            if (node?.options == null) return null;
            return node.options.FirstOrDefault(o => o != null && o.optionId == optionId);
        }

        private List<ChoicesStep.Item> BuildChoiceItems(SentenceNodeModel node)
        {
            var items = new List<ChoicesStep.Item>();
            if (node?.options == null) return items;

            int display = 0;
            foreach (var opt in node.options)
            {
                if (opt == null || string.IsNullOrEmpty(opt.optionId)) continue;
                bool allowed = EvaluateConditionsSafe(opt.conditions);
                items.Add(new ChoicesStep.Item(opt.optionId, ++display, opt.displayTextKey, allowed));
            }
            return items;
        }

        // ───────────────────────────────────────────────────────────────────────
        // Conditions / Actions
        // ───────────────────────────────────────────────────────────────────────

        private bool EvaluateConditionsSafe(IList<DialogueCondition> list)
        {
            if (list == null || list.Count == 0) return true;
            for (int i = 0; i < list.Count; i++)
            {
                var cond = list[i];
                if (cond == null) continue;
                try { if (!InvokeCondition(cond)) return false; }
                catch (Exception ex) { Debug.LogError($"[DialogueRunner] Condition threw: {cond?.name} — {ex}"); return false; }
            }
            return true;
        }

        private bool EvaluateConditionsSafe(DialogueCondition[] arr)
        {
            if (arr == null || arr.Length == 0) return true;
            for (int i = 0; i < arr.Length; i++)
            {
                var cond = arr[i];
                if (cond == null) continue;
                try { if (!InvokeCondition(cond)) return false; }
                catch (Exception ex) { Debug.LogError($"[DialogueRunner] Condition threw: {cond?.name} — {ex}"); return false; }
            }
            return true;
        }

        private void ExecuteActionsSafe(IList<DialogueAction> list)
        {
            if (list == null || list.Count == 0) return;
            for (int i = 0; i < list.Count; i++)
            {
                var act = list[i];
                if (act == null) continue;
                try { InvokeAction(act); }
                catch (Exception ex) { Debug.LogError($"[DialogueRunner] Action threw: {act?.name} — {ex}"); }
            }
        }

        private void ExecuteActionsSafe(DialogueAction[] arr)
        {
            if (arr == null || arr.Length == 0) return;
            for (int i = 0; i < arr.Length; i++)
            {
                var act = arr[i];
                if (act == null) continue;
                try { InvokeAction(act); }
                catch (Exception ex) { Debug.LogError($"[DialogueRunner] Action threw: {act?.name} — {ex}"); }
            }
        }

        private bool InvokeCondition(DialogueCondition cond)
        {
            var t = cond.GetType();
            var m = t.GetMethod("Evaluate", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, _sigEvalCtx, null);
            if (m != null) return (bool)m.Invoke(cond, new object[] { Context });

            m = t.GetMethod("Evaluate", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, _sigEvalNo, null);
            if (m != null) return (bool)m.Invoke(cond, null);

            // par défaut : true
            return true;
        }

        private void InvokeAction(DialogueAction act)
        {
            var t = act.GetType();
            var m = t.GetMethod("Execute", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, _sigExecCtx, null);
            if (m != null) { m.Invoke(act, new object[] { Context }); return; }

            m = t.GetMethod("Execute", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, _sigExecNo, null);
            if (m != null) { m.Invoke(act, null); return; }
        }

        // ───────────────────────────────────────────────────────────────────────
        // Guards
        // ───────────────────────────────────────────────────────────────────────
        private void EnsureIdle()
        {
            if (State != RunnerState.Idle && State != RunnerState.Ended)
                throw new InvalidOperationException("Runner already started");
        }
    }
}
