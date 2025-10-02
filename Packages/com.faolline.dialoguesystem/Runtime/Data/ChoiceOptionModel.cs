using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace com.faolline.dialoguesystem
{
    [Serializable]
    public class ChoiceOptionModel
    {
        public string optionId;
        public string displayTextKey;
        public DialogueCondition[] conditions; // Conditions to show this option
        public DialogueAction[] sideEffects; // Actions to perform when this option is chosen
    }

}
