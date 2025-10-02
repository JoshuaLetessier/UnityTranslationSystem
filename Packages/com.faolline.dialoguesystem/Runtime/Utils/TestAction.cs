using UnityEngine;


namespace com.faolline.dialoguesystem
{

    [UnityEngine.CreateAssetMenu(fileName = "TestAction", menuName = "Dialogue System/Actions/Test Action")]
    public class TestAction : DialogueAction
    {
        public override void Execute(IContext ctx)
        {
            Debug.LogWarning("Test Action executed.");
        }
    }
}
