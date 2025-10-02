// DialogueAction.cs
using UnityEngine;

namespace com.faolline.dialoguesystem
{
    public abstract class DialogueAction : ScriptableObject
    {
        // Exécuté par le runner. Laisse IContext minimal ou à définir plus tard.
        public abstract void Execute(IContext ctx);
    }
}
