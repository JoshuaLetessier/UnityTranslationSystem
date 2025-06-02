using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

namespace com.faolline.translationsystem
{
    [RequireComponent(typeof(TMP_Dropdown))]
    public class TranslateObjectDropdown : TranslateObject
    {
        [System.Serializable]
        private struct OptionTranslation
        {
            public string translationKey;
        }

        [SerializeField]
        private List<OptionTranslation> options = new();

        private TMP_Dropdown dropdown;

        private void Awake()
        {
            dropdown = GetComponent<TMP_Dropdown>();
        }

        public override void UpdateLanguage(SupportedLanguage newLanguage)
        {
            if (dropdown == null)
                dropdown = GetComponent<TMP_Dropdown>();

            if (dropdown == null)
            {
                Debug.LogWarning("Dropdown component not found on GameObject.");
                return;
            }

            dropdown.options.Clear();

            foreach (var opt in options)
            {
                string value = TranslationService.Get(newLanguage, opt.translationKey);
                if (!string.IsNullOrEmpty(value))
                    dropdown.options.Add(new TMP_Dropdown.OptionData(value));
            }

            dropdown.RefreshShownValue();
        }


        public void SetKey(int index, string newKey)
        {
            if (index >= 0 && index < options.Count)
            {
                options[index] = new OptionTranslation { translationKey = newKey };
                UpdateLanguage(LanguageManager.Instance?.GetCurrentLanguage() ?? SupportedLanguage.EN);
            }
        }
    }
}
