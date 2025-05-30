namespace com.faolline.translationsystem
{
    public enum SupportedLanguage
    {
        EN,
        FR,
        ES,
        DE,
        IT,
        PT,
        NL,
        SV,
        DA,
        NO,
        FI,
        PL,
        CS,
        SK,
        RO,
        HU,
        TR,
        ID,
        FIL
    }

    public static class SupportedLanguageExtensions
    {
        public static string GetNativeName(this SupportedLanguage lang)
        {
            return lang switch
            {
                SupportedLanguage.EN => "English",
                SupportedLanguage.FR => "Français",
                SupportedLanguage.ES => "Español",
                SupportedLanguage.DE => "Deutsch",
                SupportedLanguage.IT => "Italiano",
                SupportedLanguage.PT => "Português",
                SupportedLanguage.NL => "Nederlands",
                SupportedLanguage.SV => "Svenska",
                SupportedLanguage.DA => "Dansk",
                SupportedLanguage.NO => "Norsk",
                SupportedLanguage.FI => "Suomi",
                SupportedLanguage.PL => "Polski",
                SupportedLanguage.CS => "Čeština",
                SupportedLanguage.SK => "Slovenčina",
                SupportedLanguage.RO => "Română",
                SupportedLanguage.HU => "Magyar",
                SupportedLanguage.TR => "Türkçe",
                SupportedLanguage.ID => "Bahasa Indonesia",
                SupportedLanguage.FIL => "Filipino",
                _ => lang.ToString()
            };
        }
    }
}
