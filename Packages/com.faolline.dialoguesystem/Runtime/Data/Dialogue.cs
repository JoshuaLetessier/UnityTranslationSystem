using System;
using System.Collections.Generic;
using UnityEngine;


namespace com.faolline.dialoguesystem
{
    [CreateAssetMenu(menuName = "Dialogue System/Dialogue", fileName = "NewDialogue")]
    public class Dialogue : ScriptableObject
    {
        [SerializeField, HideInInspector] private string dialogueId;
        [SerializeField] private string title = "New Dialogue";
        [SerializeField] private int version = 1;
        [SerializeField] private List<Speaker> speakers = new List<Speaker>();
        [SerializeField] private string startSentenceId;
        [SerializeField] private DialogueGraphModel graph = new DialogueGraphModel();

        [SerializeField] private List<string> allTranslationKeys = new List<string>();

        [SerializeField, HideInInspector] private string assetGuid;
        public string DialogueId => dialogueId;
        public string Title { get => title; set => title = value; }
        public int Version { get => version; set => version = value; }
        public List<Speaker> Speakers => speakers;
        public string StartSentenceId { get => startSentenceId; set => startSentenceId = value; }
        public DialogueGraphModel Graph => graph;
        public List<string> AllTranslationKeys { get => allTranslationKeys; set => allTranslationKeys = value; }
        public string AssetGuid => assetGuid;


        private void OnValidate()
        {
#if UNITY_EDITOR
            var path = UnityEditor.AssetDatabase.GetAssetPath(this);
            assetGuid = UnityEditor.AssetDatabase.AssetPathToGUID(path);
#endif

            // Assure un ID stable et sérialisé
            if (string.IsNullOrEmpty(dialogueId))
                dialogueId = Guid.NewGuid().ToString("N");

            // Assure que chaque nœud / option a un ID
            if (graph?.nodes != null)
            {
                var seenNodeIds = new HashSet<string>();
                foreach (var node in graph.nodes)
                {
                    if (node == null) continue;

                    if (string.IsNullOrWhiteSpace(node.id) || !seenNodeIds.Add(node.id))
                    {
                        node.id = Guid.NewGuid().ToString("N");
                        seenNodeIds.Add(node.id);
                    }

                    if (node.options != null)
                    {
                        var seenOptionIds = new HashSet<string>();
                        foreach (var opt in node.options)
                        {
                            if (opt == null) continue;
                            if (string.IsNullOrWhiteSpace(opt.optionId) || !seenOptionIds.Add(opt.optionId))
                            {
                                opt.optionId = Guid.NewGuid().ToString("N");
                                seenOptionIds.Add(opt.optionId);
                            }
                        }
                    }
                }
            }

            // StartSentenceId auto si vide et qu’il y a au moins un nœud Start
            if (string.IsNullOrEmpty(startSentenceId))
            {
                var startNode = graph?.nodes?.Find(n => n != null && n.type == SentenceType.Start);
                if (startNode != null) startSentenceId = startNode.id;
            }
        }
    }

    public enum SentenceType { Start, Statement, Choice, End, Conditional, Action }
}
