using System.Collections.Generic;
using UnityEngine;
using com.faolline.translationsystem;
using com.faolline.dialoguesystem;

namespace com.faolline.dialoguesystem
{
    public class DialogueUIAdapter : MonoBehaviour
    {
        [Header("Bindings")]
        [SerializeField] private TranslateDialogueLine line;
        [SerializeField] private List<TranslateDialogueChoiceText> choiceSlots = new List<TranslateDialogueChoiceText>();
        [SerializeField] private GameObject choicesContainer;

        public void ShowLine(LineStep step)
        {
            if (choicesContainer) choicesContainer.SetActive(false);
            DeactivateAllChoices();

            if (line != null)
            {
                line.SetLineKey(step?.textKey ?? string.Empty);
                line.SetSpeakerId(step?.speakerId ?? string.Empty);
            }
        }

        public void ShowChoices(ChoicesStep step)
        {
            if (line != null) line.SetLineKey(string.Empty);
            if (choicesContainer) choicesContainer.SetActive(true);

            DeactivateAllChoices();
            if (step == null || step.items == null) return;

            for (int i = 0; i < choiceSlots.Count; i++)
            {
                var slot = choiceSlots[i];
                if (slot == null) continue;

                bool active = i < step.items.Count && step.items[i] != null;
                slot.gameObject.SetActive(active);
                if (!active) continue;

                var it = step.items[i];
                slot.SetMeta(it.optionId, it.index);
                slot.SetKey(it.textKey);
            }
        }

        public void HideAll()
        {
            if (choicesContainer) choicesContainer.SetActive(false);
            if (line != null) line.SetLineKey(string.Empty);
            DeactivateAllChoices();
        }

        private void DeactivateAllChoices()
        {
            foreach (var c in choiceSlots)
                if (c && c.gameObject.activeSelf) c.gameObject.SetActive(false);
        }
    }
}
