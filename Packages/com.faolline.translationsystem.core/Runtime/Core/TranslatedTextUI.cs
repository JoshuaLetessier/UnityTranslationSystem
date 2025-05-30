using TMPro;
using UnityEngine;
using Scope.Faolline.Translation;

[RequireComponent(typeof(TextMeshProUGUI))]
public class TranslatedTextUI : MonoBehaviour, ITranslatableElement
{
    [SerializeField] private string translationKey;

    private TextMeshProUGUI text;
    private ITranslationService translationService;

    private void Awake()
    {
        text = GetComponent<TextMeshProUGUI>();
        translationService = TranslationLocator.Service;
    }

    private void OnEnable()
    {
        translationService.OnLanguageChanged += RefreshTranslation;
        RefreshTranslation(translationService.CurrentLanguage);
    }

    private void OnDisable()
    {
        translationService.OnLanguageChanged -= RefreshTranslation;
    }

    public void RefreshTranslation(LanguageCode lang)
    {
        if (!string.IsNullOrEmpty(translationKey))
        {
            text.text = translationService.Get(translationKey, lang);
        }
    }
}
