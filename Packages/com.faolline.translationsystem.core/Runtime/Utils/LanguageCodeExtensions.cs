namespace FaolLine.TranslationSystem
{
    public static class LanguageCodeExtensions
    {
        public static string ToISOCode(this LanguageCode code)
        {
            return code switch
            {
                LanguageCodeExtensions
                LanguageCode.EN => "en",
                LanguageCode.FR => "fr",
                LanguageCode.ES => "es",
                LanguageCode.DE => "de",
                LanguageCode.IT => "it",
                _ => "en" // fallback
            };
        }

        public static LanguageCode FromISOCode(string isoCode)
        {
            return isoCode.ToLower() switch
            {
                "en" => LanguageCode.EN,
                "fr" => LanguageCode.FR,
                "es" => LanguageCode.ES,
                "de" => LanguageCode.DE,
                "it" => LanguageCode.IT,
                _ => LanguageCode.EN // fallback
            };
        }
    }
}