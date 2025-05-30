namespace FaolLine.TranslationSystem
{
    public class TranslationManager : ITranslationService
    {
        private readonly Dictionary<string, Dictionary<LanguageCode, string>> _translations = new();
        private LanguageCode _currentLanguage = LanguageCode.EN;

        public event Action<LanguageCode> OnLanguageChanged;

        public LanguageCode CurrentLanguage => _currentLanguage;

        public void SetLanguage(LanguageCode language)
        {
            if (_currentLanguage == language) return;
            _currentLanguage = language;
            OnLanguageChanged?.Invoke(language);
        }

        public string Get(string key)
        {
            return Get(key, _currentLanguage);
        }

        public string Get(string key, LanguageCode language)
        {
            if (_translations.TryGetValue(key, out var localizedValues) &&
                localizedValues.TryGetValue(language, out var value))
            {
                return value;
            }

            // fallback vers EN
            if (localizedValues != null && localizedValues.TryGetValue(LanguageCode.EN, out var fallback))
                return fallback;

            return key;
        }

        public void LoadTranslations(Dictionary<string, Dictionary<LanguageCode, string>> data)
        {
            _translations.Clear();
            foreach (var entry in data)
            {
                _translations[entry.Key] = new Dictionary<LanguageCode, string>(entry.Value);
            }
        }

        public bool HasKey(string key) => _translations.ContainsKey(key);
    }
}