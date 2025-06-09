using System;
using UnityEngine;

namespace com.faolline.translationsystem
{
    [Serializable]
    public struct AudioTranslation
    {
        public SupportedLanguage language;
        public AudioClip clip;
    }

    [RequireComponent(typeof(AudioSource))]
    public class TranslateObjectAudio : TranslateObject
    {
        [SerializeField]
        private AudioTranslation[] translations;

        private AudioSource audioSource;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
        }

        public override void UpdateLanguage(SupportedLanguage newLanguage)
        {
            foreach (var t in translations)
            {
                if (t.language == newLanguage)
                {
                    audioSource.clip = t.clip;
                    return;
                }
            }

            Debug.LogWarning($"No audio clip found for language: {newLanguage}");
        }

        public void Play() => audioSource?.Play();
    }

}
