namespace FaolLine.TranslationSystem
{
    /// <summary>Interface pour les éléments qui peuvent être traduits.</summary>
    /// <remarks>Cette interface est utilisée pour les éléments qui doivent être mis à jour lorsque la langue change.</remarks>
    /// <example>Un exemple d'implémentation de cette interface serait un bouton qui doit changer son texte lorsque la langue change.</example>
    /// <seealso cref="ITranslatableElement"/>
    /// <seealso cref="TranslationManager"/>
    /// <seealso cref="LanguageManager"/>
    public interface ITranslatableElement
    {
        /// <summary>Actualise l'affichage ou la logique en fonction de la nouvelle langue.</summary>
        void RefreshTranslation(LanguageCode languageCode);
    }
}