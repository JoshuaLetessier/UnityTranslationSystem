using UnityEngine;
using TMPro;

namespace com.faolline.translationsystem
{
    /// Affiche la ligne courante (clé → texte localisé). Facultatif : nom du speaker.
    public class TranslateDialogueLine : TranslateObject
    {
        [Header("Main")]
        [SerializeField] private TMP_Text lineText;

        [Header("Speaker (optional)")]
        [SerializeField] private bool showSpeakerName = false;
        [SerializeField] private TMP_Text speakerText;
        [SerializeField] private string speakerKeyPrefix = "speaker."; // ex: speaker.guard

        private string _lineKey;
        private string _speakerKey;

        public void SetLineKey(string key)
        {
            _lineKey = key ?? string.Empty;
            UpdateLanguage(LanguageManager.Instance?.GetCurrentLanguage() ?? SupportedLanguage.EN);
        }

        public void SetSpeakerId(string speakerId)
        {
            if (!showSpeakerName) return;
            _speakerKey = string.IsNullOrEmpty(speakerId) ? string.Empty : speakerKeyPrefix + speakerId;
            UpdateLanguage(LanguageManager.Instance?.GetCurrentLanguage() ?? SupportedLanguage.EN);
        }

        public override void UpdateLanguage(SupportedLanguage newLanguage)
        {
            if (lineText == null) lineText = GetComponent<TMP_Text>();

            if (!string.IsNullOrEmpty(_lineKey))
            {
                var value = TranslationService.Get(newLanguage, _lineKey);
                lineText.text = string.IsNullOrEmpty(value) ? _lineKey : value;
            }

            if (showSpeakerName && speakerText != null && !string.IsNullOrEmpty(_speakerKey))
            {
                var value = TranslationService.Get(newLanguage, _speakerKey);
                speakerText.text = string.IsNullOrEmpty(value) ? _speakerKey : value;
            }
        }
    }
}
