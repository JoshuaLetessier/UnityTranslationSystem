namespace com.faolline.translationsystem
{
    public interface ITranslationService
    {
        /// <summary>Retourne la traduction pour une clé donnée dans la langue courante.</summary>
        string Get(string key);

        /// <summary>Retourne la traduction dans une langue spécifique, ou fallback si introuvable.</summary>
        string Get(string key, LanguageCode  languageCode);

        /// <summary>Langue actuellement active dans le jeu.</summary>
        string CurrentLanguage { get; }

        /// <summary>Change la langue active et notifie les éléments abonnés.</summary>
        void SetLanguage(LanguageCode languageCode);

        /// <summary>Événement déclenché lors d’un changement de langue.</summary>
        event Action<LanguageCode> OnLanguageChanged;
    }
}