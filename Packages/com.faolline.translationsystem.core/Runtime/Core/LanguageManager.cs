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

        private SupportedLanguage defaultLaugage;


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

            // Ne pas écraser si déjà assignée (car sérialisée)
            if (!languageDataBase.IsLanguageEnabled(currentLanguage))
            {
                currentLanguage = GetDefaultLanguage(); // fallback
            }

            TranslationService.ReloadAll(languageDataBase.EnabledLanguages.ToList());
            RefreshAll();
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
                TranslationService.ReloadAll(languageDataBase.EnabledLanguages.ToList()); // recharge les traductions si modifiées
                RefreshAll();
            }
        }

        /// Method to get the current language
        ///     
        /// <returns>The current language code</returns>
        /// 
        public string GetCurrentLanguage() => currentLanguage.ToString();

        public void SetLanguageFromCode(string code)
        {
            if (System.Enum.TryParse(code, out SupportedLanguage lang))
            {
                ChangeLanguage(code);
            }
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

        public void ForceSetCurrentLanguage(SupportedLanguage lang)
        {
            currentLanguage = lang;
            #if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    // Éditeur simulate refresh
                    var translatedObjects = FindObjectsByType<TranslateObject>(FindObjectsSortMode.None);
                    foreach (var obj in translatedObjects)
                    {
                        obj.UpdateLanguage(lang);
                    }
                    return;
                }  
            #endif

            // Play mode
            TranslationService.ReloadAll(languageDataBase.EnabledLanguages.ToList());
            RefreshAll();
        }

    }
}