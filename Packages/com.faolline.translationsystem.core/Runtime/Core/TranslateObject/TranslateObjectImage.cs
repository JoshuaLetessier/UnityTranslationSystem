namespace com.faolline.translationsystem
{
    public class TranslateObjectImage : TranslateObject
    {
        [SerializeField] private string translationKey;
        private Image image;

        private void Awake()
        {
            image = GetComponent<Image>();
        }

        public override void UpdateLanguage(SupportedLanguage lang)
        {
            string path = TranslationLoader.GetTranslation(lang, translationKey);
            if (!string.IsNullOrEmpty(path))
            {
                Sprite sprite = Resources.Load<Sprite>(path);
                if (sprite != null)
                    image.sprite = sprite;
            }
        }
    }
}
