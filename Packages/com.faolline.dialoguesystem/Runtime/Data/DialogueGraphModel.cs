using System;
using System.Collections.Generic;
using UnityEngine;

namespace com.faolline.dialoguesystem
{
    [Serializable]
    public class DialogueGraphModel
    {
        public List<SentenceNodeModel> nodes = new List<SentenceNodeModel>();
        public List<DialogueEdgeModel> edges = new List<DialogueEdgeModel>();
        public Vector3 viewPosition;
        public float viewScale = 1f;

        public SentenceNodeModel GetNodeById(string nodeId)
        {
            return nodes.Find(n => n.id == nodeId);
        }

        public SentenceNodeModel GetNodeByType(SentenceType type)
        {
            return nodes.Find(n => n.type == type);
        }

        public DialogueEdgeModel GetEdgeByNodeId(string edgeId)
        {
            return edges.Find(e => e.fromNodeId == edgeId);
        }
    }
}
