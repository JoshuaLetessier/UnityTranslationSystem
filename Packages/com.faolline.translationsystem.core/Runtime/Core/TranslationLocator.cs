namespace Scope.Faolline.Translation
{
    public static class TranslationLocator
    {
        public static ITranslationService Service { get; private set; }

        public static void Bind(ITranslationService service)
        {
            Service = service;
        }
    }
}
