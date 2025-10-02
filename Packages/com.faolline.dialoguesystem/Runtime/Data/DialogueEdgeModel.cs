using System;

namespace com.faolline.dialoguesystem
{
    [Serializable]
    public class DialogueEdgeModel
    {
        public string fromNodeId;
        public string toNodeId;
        public string portName; // "Next" ou "Option:<optionId>"
    }

}
