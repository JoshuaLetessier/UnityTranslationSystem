using UnityEngine;
using UnityEngine.UI;
using System;

namespace com.faolline.translationsystem
{
    [RequireComponent(typeof(Image))]
    public class TranslateObjectImage : TranslateObject
    {
        [Serializable]
        public struct ImageTranslation
        {
            public SupportedLanguage language;
            public Sprite sprite;
        }

        [SerializeField]
        private ImageTranslation[] translations;

        [SerializeField]
        private Sprite defaultSprite;

        private Image imageComponent;

        private void Awake()
        {
            imageComponent = GetComponent<Image>();
            if (imageComponent == null)
            {
                Debug.LogError("Image component not found on GameObject.");
            }
        }

        public override void UpdateLanguage(SupportedLanguage newLanguage)
        {
            if (imageComponent == null) return;

            foreach (var translation in translations)
            {
                if (translation.language == newLanguage)
                {
                    imageComponent.sprite = translation.sprite;
                    return;
                }
            }

            if (defaultSprite != null)
            {
                imageComponent.sprite = defaultSprite;
                Debug.LogWarning($"No translation found for language: {newLanguage}, fallback to default sprite.");
            }
            else
            {
                Debug.LogWarning($"No translation found for language: {newLanguage}, and no default sprite defined.");
            }
        }
    }
}
