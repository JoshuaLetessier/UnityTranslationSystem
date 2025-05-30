using UnityEngine;

namespace com.faolline.translationsystem
{
    public abstract class TranslateObject : MonoBehaviour
    {

        private void Awake()
        {
            LanguageManager languageManager = LanguageManager.Instance;
            if (languageManager == null)
            {
                Debug.LogError("LanguageManager instance is not available.");
                return;
            }
            UpdateLanguage(languageManager.GetCurrentLanguage());
        }

        public abstract void UpdateLanguage(SupportedLanguage newLanguage);
        public virtual void OnEnable()
        {
            if(LanguageManager.Instance)
                LanguageManager.Instance.Register(this);
        }
        public virtual void OnDisable()
        {
            if (LanguageManager.Instance)
                LanguageManager.Instance.Unregister(this);
        }
    }
}
