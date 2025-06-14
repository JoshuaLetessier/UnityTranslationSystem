using System;
using com.faolline.translationsystem;

namespace com.faolline.translationsystem
{
    [Serializable]
    public class TranslationSaveData
    {
        public string selectedLanguage;

        public TranslationSaveData() { }

        public TranslationSaveData(SupportedLanguage language)
        {
            selectedLanguage = language.ToString();
        }

        public SupportedLanguage? GetLanguage()
        {
            if (Enum.TryParse(selectedLanguage, out SupportedLanguage lang))
                return lang;

            return null;
        }
    }
}


//Utilisation example:
////Save
////var data = new TranslationSaveData(LanguageManager.Instance.GetCurrentLanguage());
////SaveSystem.SaveData("translation", data);
////
////Load
////var data = ...
////if (data?.GetLanguage() is SupportedLanguage lang)
////{
////    LanguageManager.Instance.ForceSetCurrentLanguage(lang);
////}