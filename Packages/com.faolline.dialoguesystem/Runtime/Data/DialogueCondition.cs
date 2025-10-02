// DialogueCondition.cs
using UnityEngine;

namespace com.faolline.dialoguesystem
{
    public abstract class DialogueCondition : ScriptableObject
    {
        public abstract bool Evaluate(IContext ctx);
    }
}

