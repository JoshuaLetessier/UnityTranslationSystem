using System;

namespace com.faolline.dialoguesystem
{
    public struct PortTag
    {
        public string nodeId;     // ID du node DTO
        public string role;       // "In", "Next", "True", "False", "Option"
        public string optionId;   // si role == "Option"
        public PortTag(string nodeId, string role, string optionId = null)
        { this.nodeId = nodeId; this.role = role; this.optionId = optionId; }
    }
}
