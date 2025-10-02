using UnityEngine;

namespace com.faolline.dialoguesystem
{
    [CreateAssetMenu(fileName = "TestCondition", menuName = "Dialogue System/Conditions/Test Condition")]
    public class TestCondition : DialogueCondition
    {
        [SerializeField] private string testString;
        public override bool Evaluate(IContext ctx)
        {
            Debug.Log($"Test Condition evaluated with string: {testString}");
            return true;
        }
    }
}
