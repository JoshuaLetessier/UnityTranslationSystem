
namespace com.faolline.translationsystem
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
