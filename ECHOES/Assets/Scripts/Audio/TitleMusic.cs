using System.Collections;
using UnityEngine;
using Echoes.Menus;

namespace Echoes.Audio
{
    // Música de fondo del Título: bucle simple con fundido de entrada, sin salas ni fundidos
    // cruzados (eso es cosa de AudioManager, que solo existe en la escena de juego). Respeta el
    // mismo volumen de Música que guarda OptionsMenu, y se actualiza en vivo si el slider se
    // mueve con Opciones abierta desde el propio Título.
    public class TitleMusic : MonoBehaviour
    {
        public static TitleMusic Instance { get; private set; }

        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioClip clip;
        [SerializeField] private float fadeInTime = 1f;
        [Tooltip("Tope real: el slider de Música al 100% suena a este volumen, no al del clip tal cual.")]
        [Range(0f, 1f)] [SerializeField] private float maxVolume = 0.5f;

        private void Awake()
        {
            Instance = this;
            if (musicSource != null)
            {
                musicSource.loop = true;
                musicSource.clip = clip;
                musicSource.volume = 0f;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            if (musicSource == null || clip == null) return;
            musicSource.Play();
            StartCoroutine(FadeIn());
        }

        private IEnumerator FadeIn()
        {
            float target = PlayerPrefs.GetFloat(OptionsMenu.MusicVolumeKey, 1f) * maxVolume;
            float t = 0f;
            while (t < fadeInTime)
            {
                t += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(0f, target, fadeInTime > 0f ? t / fadeInTime : 1f);
                yield return null;
            }
            musicSource.volume = target;
        }

        // 'sliderValue' llega SIN escalar (0-1 tal cual el slider) desde OptionsMenu.
        public void SetVolume(float sliderValue)
        {
            if (musicSource != null) musicSource.volume = sliderValue * maxVolume;
        }
    }
}
