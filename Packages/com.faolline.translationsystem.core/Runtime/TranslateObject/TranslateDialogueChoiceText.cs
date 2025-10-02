using UnityEngine;
using TMPro;

namespace com.faolline.translationsystem
{
    /// Libellé d’un choix (clé → texte localisé). Garde optionId/index si tu veux t’en servir côté UI.
    public class TranslateDialogueChoiceText : TranslateObject
    {
        [SerializeField] private TMP_Text label;

        private string _key;
        public string OptionId { get; private set; }
        public int DisplayIndex { get; private set; } // 1..N

        public void SetKey(string key)
        {
            _key = key ?? string.Empty;
            UpdateLanguage(LanguageManager.Instance?.GetCurrentLanguage() ?? SupportedLanguage.EN);
        }

        public void SetMeta(string optionId, int displayIndex)
        {
            OptionId = optionId;
            DisplayIndex = displayIndex;
        }

        public override void UpdateLanguage(SupportedLanguage newLanguage)
        {
            if (label == null) label = GetComponent<TMP_Text>();

            if (!string.IsNullOrEmpty(_key))
            {
                var value = TranslationService.Get(newLanguage, _key);
                label.text = string.IsNullOrEmpty(value) ? _key : value;
            }
        }
    }
}
