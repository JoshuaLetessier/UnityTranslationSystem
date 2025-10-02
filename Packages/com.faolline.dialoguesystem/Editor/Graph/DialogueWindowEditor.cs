using UnityEditor;
using UnityEngine;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.UIElements;
using Unity.VisualScripting;
using System;
using static UnityEditor.Experimental.GraphView.GraphView;

namespace com.faolline.dialoguesystem
{
    public class DialogueWindowEditor : SingletonEditorWindow<DialogueWindowEditor>
    {
        [MenuItem("Window/Dialogue Graph/New Empty")]
        public static void CreateAndOpen()
        {
            var asset = ScriptableObject.CreateInstance<Dialogue>();

            var path = EditorUtility.SaveFilePanelInProject("Create Dialogue", "NewDialogue", "asset", "Choose location");
            if (string.IsNullOrEmpty(path)) return;
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            Open(asset);
        }

        public static void Open(Dialogue dialogue)
        {
            var window = GetWindow<DialogueWindowEditor>();
            window.titleContent = new GUIContent($"Dialogue: {dialogue.name}");
            window.Show();
            window.LoadDialogue(dialogue);
        }

        private Dialogue currentDialogue;
        private CustomGraphView graph;
        private MiniMap miniMap;
        private SentenceInspectorView inspector;
        private TwoPaneSplitView split;
        private VisualElement emptyState;

        private Dictionary<string, Node> nodeViewById = new();

        private void OnEnable()
        {
            rootVisualElement.style.flexDirection = FlexDirection.Column;

            var toolbar = new Toolbar();
            var saveBtn = new ToolbarButton(Save) { text = "Save" };
            toolbar.Add(saveBtn);
            rootVisualElement.Add(toolbar);

            emptyState = new Label("Select or create a Dialogue asset to edit.");
            emptyState.style.unityTextAlign = TextAnchor.MiddleCenter;
            emptyState.style.flexGrow = 1;
            rootVisualElement.Add(emptyState);
        }

        private void CreateGraphView()
        {
            split = new TwoPaneSplitView(0, 1200, TwoPaneSplitViewOrientation.Horizontal);
            rootVisualElement.Add(split);
            graph = new CustomGraphView();



            split.Add(graph);

            inspector = new SentenceInspectorView();
            inspector.style.minWidth = 280;
            inspector.style.width = 320;
            inspector.style.flexGrow = 0;
            inspector.style.flexShrink = 0;
            split.Add(inspector);

            // Bridge : le GraphView demande la création au Window (qui gère le modèle/Undo)
            graph.onRequestCreateNode = OnRequestCreateNode;
            graph.OnSelectionChanged = OnGraphSelectionChanged;


            miniMap = new MiniMap { anchored = true };
            miniMap.SetPosition(new Rect(10, 30, 180, 120));
            graph.Add(miniMap);



            if (currentDialogue != null)
            {
                graph.Bind(currentDialogue);
                inspector.SetAsset(currentDialogue);
            }
        }

        private void ClearGraphUI()
        {
            // détacher le split (un seul Remove)
            if (split != null)
            {
                split.RemoveFromHierarchy();
                split = null;
            }

            // nuller les refs
            graph = null;
            inspector = null;
        }

        public void LoadDialogue(Dialogue dialogue)
        {
            currentDialogue = dialogue;
            titleContent = new GUIContent($"Dialogue: {dialogue.name}");
            emptyState?.RemoveFromHierarchy();
            ClearGraphUI();
            CreateGraphView();

            inspector?.SetAsset(currentDialogue);

            // Auto-setup si graphe vide : Start + Statement + Choice + End
            if (currentDialogue.Graph.nodes.Count == 0)
            {
                Undo.RecordObject(currentDialogue, "Initialize Dialogue Graph");

                // Positions par défaut
                var pStart = new Vector2(100, 200);
                var pStmt = new Vector2(350, 180);
                var pChoice = new Vector2(600, 160);
                var pEnd = new Vector2(900, 200);

                var nStart = CreateNodeModel(SentenceType.Start, pStart);
                currentDialogue.StartSentenceId = nStart.id.ToString();

                var nStmt = CreateNodeModel(SentenceType.Statement, pStmt);
                var nChoice = CreateNodeModel(SentenceType.Choice, pChoice);
                var nEnd = CreateNodeModel(SentenceType.End, pEnd);

                // Ajoute 2 options par défaut au Choice
                nChoice.options.Add(new ChoiceOptionModel { optionId = System.Guid.NewGuid().ToString(), displayTextKey = "" });
                nChoice.options.Add(new ChoiceOptionModel { optionId = System.Guid.NewGuid().ToString(), displayTextKey = "" });

                // Edges par défaut : Start->Statement, Statement->Choice, Option1->End
                currentDialogue.Graph.edges.Add(new DialogueEdgeModel { fromNodeId = nStart.id, toNodeId = nStmt.id, portName = "Next" });
                currentDialogue.Graph.edges.Add(new DialogueEdgeModel { fromNodeId = nStmt.id, toNodeId = nChoice.id, portName = "Next" });
                currentDialogue.Graph.edges.Add(new DialogueEdgeModel { fromNodeId = nChoice.id, toNodeId = nEnd.id, portName = $"Option:{nChoice.options[0].optionId}" });

                EditorUtility.SetDirty(currentDialogue);
                AssetDatabase.SaveAssets();
            }

            RebuildFromModel();
        }

        private SentenceNodeModel CreateNodeModel(SentenceType type, Vector2 position)
        {
            var model = new SentenceNodeModel
            {
                id = System.Guid.NewGuid().ToString(),
                type = type,
                position = position,
                size = new Vector2(220, 120)
            };
            if (type == SentenceType.Choice)
                model = new SentenceNodeModel
                {
                    id = model.id,
                    type = model.type,
                    position = model.position,
                    size = model.size,
                    options = new List<ChoiceOptionModel>()
                };


            currentDialogue.Graph.nodes.Add(model);
            return model;
        }

        private void OnRequestCreateNode(SentenceType type, Vector2 graphMousePos)
        {
            if (currentDialogue == null) return;

            // Unicité du Start
            if (type == SentenceType.Start && currentDialogue.Graph.nodes.Any(n => n.type == SentenceType.Start))
            {
                EditorUtility.DisplayDialog("Dialogue Graph", "Un Start existe déjà dans ce graphe.", "OK");
                return;
            }

            Undo.RecordObject(currentDialogue, "Add Node");
            var model = CreateNodeModel(type, graphMousePos);

            // Start → définit StartSentenceId si absent
            if (type == SentenceType.Start && string.IsNullOrEmpty(currentDialogue.StartSentenceId))
                currentDialogue.StartSentenceId = model.id.ToString();

            EditorUtility.SetDirty(currentDialogue);

            // Ajoute la vue immédiatement (pas besoin d’attendre un reload)
            AddNodeViewFromModel(model);

            // Focus/selection
            graph.ClearSelection();
            graph.AddToSelection(nodeViewById[model.id]);
            graph.FrameNext();
        }

        private void AddNodeViewFromModel(SentenceNodeModel m)
        {
            var view = CreateNodeView(m);           // instancie la bonne sous-classe
            view.userData = m.id;                   // utile pour deletes/moves/etc.
            graph.AddElement(view);                 // ajoute à la scène GraphView

            nodeViewById[m.id] = view;              // mappings
            graph.RegisterNodeView(m.id, view);
        }

        private DialogueNode CreateNodeView(SentenceNodeModel m)
        {
            switch (m.type)
            {
                case SentenceType.Start: return new StartDialogueNode(graph, m);
                case SentenceType.Statement: return new StatementDialogueNode(graph, m);
                case SentenceType.Choice: return new ChoiceDialogueNode(graph, m);
                case SentenceType.End: return new EndDialogueNode(graph, m);
                default: return new StatementDialogueNode(graph, m); // fallback
            }
        }

        private void RebuildFromModel()
        {
            if (graph == null || currentDialogue == null) return;

            graph.DeleteElements(graph.graphElements.ToList());
            nodeViewById.Clear();
            graph.Bind(currentDialogue);

            foreach (var m in currentDialogue.Graph.nodes)
                AddNodeViewFromModel(m);

            graph.BuildEdgesFromModel();

            var startId = currentDialogue.StartSentenceId;
            if (!string.IsNullOrEmpty(startId) && nodeViewById.TryGetValue(startId, out var startView))
            {
                graph.ClearSelection();
                graph.AddToSelection(startView);
                graph.FrameNext();
            }
        }

        private void OnGraphSelectionChanged(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId) || currentDialogue == null) 
            { 
                inspector?.ClearInspector(); 
                return; 
            }
      
            var model = currentDialogue.Graph.nodes.FirstOrDefault(n => n.id == nodeId);
            if (model != null) { inspector?.BindNode(model); }
            else inspector?.ClearInspector();
        }

        private void Save()
        {
            if (currentDialogue == null) return;

            var result = ValidateGraphForSave();
            if (result.HasIssues)
            {
                int orphanCount = result.orphanNodeIds.Count;
                int unlinkedCount = result.unlinkedChoicesByNode.Sum(k => k.Value.Count);

                bool proceed = EditorUtility.DisplayDialog(
                    "Dialogue Graph",
                    $"Certains éléments ne sont pas reliés :\n" +
                    $"- Nodes orphelins : {orphanCount}\n" +
                    $"- Choix non reliés : {unlinkedCount}\n\n" +
                    "Voulez-vous sauvegarder quand même ?",
                    "Oui",
                    "Non"
                );

                // Applique le visuel de warning dans tous les cas
                ApplyVisualWarnings(result);

                // Si l’utilisateur dit Non → on stoppe la sauvegarde
                if (!proceed)
                    return;
            }
            else
            {
                // Tout est propre → on nettoie les warnings résiduels
                ClearAllVisualIssuesOnGraph();
            }

            Undo.RecordObject(currentDialogue, "Edit Dialogue");
            EditorUtility.SetDirty(currentDialogue);
            AssetDatabase.SaveAssets();
        }


        //Check if all nodes are connected
        // Résultats de validation
        private struct GraphValidationResult
        {
            public HashSet<string> orphanNodeIds;                       // nodes sans aucune connexion
            public Dictionary<string, List<int>> unlinkedChoicesByNode; // nodeId -> index des options non reliées
            public bool HasIssues => (orphanNodeIds?.Count > 0) || (unlinkedChoicesByNode?.Count > 0);
        }

        // Validation complète : orphelins + options de Choice non reliées
        private GraphValidationResult ValidateGraphForSave()
        {
            var res = new GraphValidationResult
            {
                orphanNodeIds = new HashSet<string>(),
                unlinkedChoicesByNode = new Dictionary<string, List<int>>()
            };

            if (currentDialogue == null || currentDialogue.Graph == null)
                return res;

            var nodes = currentDialogue.Graph.nodes ?? new List<SentenceNodeModel>();
            var edges = currentDialogue.Graph.edges ?? new List<DialogueEdgeModel>();

            // 1) Nodes orphelins (aucune edge entrante/sortante)
            var connectedNodeIds = new HashSet<string>();
            foreach (var e in edges)
            {
                if (!string.IsNullOrEmpty(e.fromNodeId)) connectedNodeIds.Add(e.fromNodeId);
                if (!string.IsNullOrEmpty(e.toNodeId)) connectedNodeIds.Add(e.toNodeId);
            }
            foreach (var n in nodes)
            {
                if (!connectedNodeIds.Contains(n.id))
                    res.orphanNodeIds.Add(n.id);
            }

            // 2) Options de Choice non reliées (port "Option:{optionId}" absent)
            foreach (var n in nodes)
            {
                // Heuristique : un Choice a potentiellement une liste d'options
                if (n.options == null || n.options.Count == 0) continue;

                List<int> badIndexes = null;
                for (int i = 0; i < n.options.Count; i++)
                {
                    var opt = n.options[i];
                    if (opt == null || string.IsNullOrEmpty(opt.optionId))
                    {
                        (badIndexes ??= new List<int>()).Add(i);
                        continue;
                    }

                    bool hasEdge = edges.Any(e =>
                        e.fromNodeId == n.id &&
                        e.portName == $"Option:{opt.optionId}"
                    );

                    if (!hasEdge)
                        (badIndexes ??= new List<int>()).Add(i);
                }

                if (badIndexes != null && badIndexes.Count > 0)
                    res.unlinkedChoicesByNode[n.id] = badIndexes;
            }

            return res;
        }

        // Efface tous les warnings visuels
        private void ClearAllVisualIssuesOnGraph()
        {
            // Suppose que tu as accès à ta GraphView (ici _graphView)
            foreach (var view in graph.graphElements.ToList().OfType<DialogueNode>())
                view.SetIssues(Array.Empty<NodeIssue>());
        }

        // Applique les warnings visuels en fonction du résultat
        private void ApplyVisualWarnings(GraphValidationResult r)
        {
            // D’abord tout nettoyer
            ClearAllVisualIssuesOnGraph();

            // Orphelins
            foreach (var nodeId in r.orphanNodeIds)
            {
                var nodeView = graph.graphElements.OfType<DialogueNode>().FirstOrDefault(v => v.sentenceNodeModel.id == nodeId);
                if (nodeView == null) continue;

                nodeView.SetIssues(new[]
                {
            new NodeIssue
            {
                severity = IssueSeverity.Warning,
                message = "Node non relié (aucune connexion).",
                field   = ""
            }
        });
            }

            // Choices non reliés
            foreach (var kvp in r.unlinkedChoicesByNode)
            {
                var nodeId = kvp.Key;
                var idxs = kvp.Value;

                var nodeView = graph.graphElements.OfType<DialogueNode>().FirstOrDefault(v => v.sentenceNodeModel.id == nodeId);
                if (nodeView == null) continue;

                string list = string.Join(", ", idxs.Select(i => $"Option {i + 1}"));
                nodeView.SetIssues(new[]
                {
            new NodeIssue
            {
                severity = IssueSeverity.Warning,
                message  = $"Choix non relié(s) : {list}.",
                field    = "Options"
            }
        });
            }

            // Optionnel : focus sur le premier node en erreur
            var firstId = r.orphanNodeIds.FirstOrDefault();
            if (string.IsNullOrEmpty(firstId) && r.unlinkedChoicesByNode.Count > 0)
                firstId = r.unlinkedChoicesByNode.Keys.First();

            if (!string.IsNullOrEmpty(firstId))
            {
                var first = graph.graphElements.OfType<DialogueNode>().FirstOrDefault(v => v.sentenceNodeModel.id == firstId);
                if (first != null)
                {
                    graph.ClearSelection();
                    graph.AddToSelection(first);
                    graph.FrameSelection();
                }
            }
        }

    }
}
