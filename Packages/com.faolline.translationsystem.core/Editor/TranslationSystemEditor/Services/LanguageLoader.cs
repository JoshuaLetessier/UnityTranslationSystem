using System.Collections.Generic;
using System.Linq;

namespace com.faolline.translationsystem
{
    public static class LanguageLoader
    {
        public static List<SupportedLanguage> LoadLanguages()
        {
            var manager = LanguageManager.Instance;
            if (manager != null && manager.GetLanguageDataBase() != null)
            {
                return manager.GetLanguageDataBase().EnabledLanguages.ToList();
            }
            return new List<SupportedLanguage>();
        }
    }
}
