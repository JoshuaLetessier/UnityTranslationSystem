using TMPro;

namespace com.faolline.translationsystem
{
    public class TranslateObjectText : TranslateObject
    {
        [SerializeField] private string translationKey;
        private TMP_Text text;

        private void Awake()
        {
            text = GetComponent<TMP_Text>();
        }

        public override void UpdateLanguage(SupportedLanguage newLanguage)
        {
            EnsureTextComponent();

            string value = TranslationLoader.GetTranslation(newLanguage, translationKey);
            if (!string.IsNullOrEmpty(value))
                text.text = value;
        }

        public void SetKey(string key)
        {
            translationKey = key;
            UpdateLanguage(LanguageManager.Instance?.GetCurrentLanguage() ?? SupportedLanguage.EN);
        }

        private void EnsureTextComponent()
        {
            if (text == null)
                text = GetComponent<TMP_Text>();
        }

        public string GetKey() => translationKey;
    }
}
