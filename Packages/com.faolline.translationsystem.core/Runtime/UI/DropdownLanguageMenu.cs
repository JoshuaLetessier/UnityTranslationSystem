using System;
using System.Linq;
using TMPro;
using UnityEngine;

namespace com.faolline.translationsystem
{
    [RequireComponent(typeof(TMP_Dropdown))]
    public class LanguageDropdown : MonoBehaviour
    {
        private LanguageManager languageManager;
        private TMP_Dropdown dropdown;

        private void Awake()
        {
            languageManager = LanguageManager.Instance;
            dropdown = GetComponent<TMP_Dropdown>();

            if (languageManager == null)
            {
                Debug.LogError("LanguageManager instance is not initialized.");
                return;
            }

            if (dropdown == null)
            {
                Debug.LogError("TMP_Dropdown component not found.");
                return;
            }

            UpdateDropdownOptions(languageManager.GetCurrentLanguage());

            dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
        }

        private void OnDestroy()
        {
            dropdown.onValueChanged.RemoveListener(OnDropdownValueChanged);
        }

        private void UpdateDropdownOptions(SupportedLanguage currentLanguage)
        {
            var langs = languageManager.GetLanguageDataBase().EnabledLanguages;

            dropdown.ClearOptions();

            var options = new System.Collections.Generic.List<TMP_Dropdown.OptionData>();
            foreach (var lang in langs)
            {
                options.Add(new TMP_Dropdown.OptionData(lang.GetNativeName()));
            }

            dropdown.AddOptions(options);

            // Sélectionne la langue courante
            int index = langs.ToList().IndexOf(currentLanguage);
            if (index >= 0)
            {
                dropdown.value = index;
            }
            else
            {
                Debug.LogWarning($"Current language {currentLanguage} is not in the enabled languages list. Falling back to first available.");

                if (langs.Count > 0)
                {
                    dropdown.value = 0;
                    languageManager.ForceSetCurrentLanguage(langs[0]); // met à jour le manager pour rester cohérent
                }
            }

            dropdown.RefreshShownValue();
        }

        private void OnDropdownValueChanged(int selectedIndex)
        {
            var langs = languageManager.GetLanguageDataBase().EnabledLanguages;
            if (selectedIndex >= 0 && selectedIndex < langs.Count)
            {
                var selectedLang = langs[selectedIndex];
                languageManager.ChangeLanguage(selectedLang.ToString());
            }
        }
    }
}
