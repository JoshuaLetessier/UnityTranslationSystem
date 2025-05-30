using System;
using System.Linq;
using UnityEngine;

namespace com.faolline.translationsystem
{
    public class LanguageManager : Singleton<LanguageManager>
    {
        protected override bool IsPersistent => true;

        private SupportedLanguage currentLanguage;

        private System.Action<SupportedLanguage> LanguageChangeEvent;

        private SupportedLanguage defaultLaugage = SupportedLanguage.EN;


        [SerializeField]
        private LanguageDataBase languageDataBase;

        protected override void Awake()
        {
            base.Awake();

            if (languageDataBase == null)
            {
                Debug.LogError("LanguageDataBase is not assigned.");
                return;
            }

            currentLanguage = defaultLaugage;
        }

        public void Register(TranslateObject tradObject)
        {
            if (tradObject != null)
            {
                tradObject.UpdateLanguage(currentLanguage);
                LanguageChangeEvent += tradObject.UpdateLanguage;
            }
            else
            {
                Debug.LogWarning("Attempted to register a null GenericTradObject.");
            }
        }

        public void Unregister(TranslateObject tradObject)
        {
            if (tradObject != null)
            {
                LanguageChangeEvent -= tradObject.UpdateLanguage;
            }
            else
            {
                Debug.LogWarning("Attempted to unregister a null GenericTradObject.");
            }
        }

        public void RefreshAll()
        {
            LanguageChangeEvent?.Invoke(currentLanguage);
        }

        /// Method to change the current language
        /// 
        /// <param name="languageCode">The code of the language to change to</param>
        /// 
        public void ChangeLanguage(string languageCode)
        {
            SupportedLanguage? newLanguage = languageDataBase.GetEnabledLanguage(languageCode);
            if (newLanguage.HasValue)
            {
                currentLanguage = newLanguage.Value;
                TranslationLoader.ReloadAll(languageDataBase.EnabledLanguages.ToList()); // recharge les traductions si modifiées
                RefreshAll();
            }
        }


        /// Method to get the current language
        ///     
        /// <returns>The current language code</returns>
        /// 
        public SupportedLanguage GetCurrentLanguage()
        {
            return currentLanguage;
        }

        public LanguageDataBase GetLanguageDataBase()
        {
            return languageDataBase;
        }

        public void SetDefaultLanguage(SupportedLanguage lang)
        {
            if (languageDataBase.IsLanguageEnabled(lang))
            {
                defaultLaugage = lang;
                Debug.Log($"Default language set to: {defaultLaugage.GetNativeName()}");
            }
            else
            {
                Debug.LogWarning($"Cannot set default language to {lang}. It is not enabled.");
            }
        }

        public SupportedLanguage GetDefaultLanguage()
        {
            if (languageDataBase.IsLanguageEnabled(defaultLaugage))
            {
                return defaultLaugage;
            }
            else
            {
                Debug.LogWarning($"Default language {defaultLaugage} is not enabled. Falling back to first available.");
                return languageDataBase.EnabledLanguages.Count > 0 ? languageDataBase.EnabledLanguages[0] : SupportedLanguage.EN;
            }
        }
    }
}