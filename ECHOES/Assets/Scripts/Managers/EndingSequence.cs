using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace Echoes.Managers
{
    public class EndingSequence : MonoBehaviour
    {
        [SerializeField] private Image overlay;
        [Tooltip("Panel del cómic de cierre. Déjalo sin sprite asignado hasta tener el arte final — la secuencia lo saltará automáticamente.")]
        [SerializeField] private Image comicImage;
        [SerializeField] private TextMeshProUGUI thanksText;
        [SerializeField] private string thanksMessage = "Gracias por jugar la demo";

        [Header("Tiempos (segundos)")]
        [SerializeField] private float fadeToBlackTime = 1f;
        [SerializeField] private float comicFadeTime = 1f;
        [SerializeField] private float comicHoldTime = 4f;
        [SerializeField] private float textFadeTime = 1f;

        private void Awake()
        {
            overlay.color = Color.clear;
            SetImageAlpha(comicImage, 0f);
            thanksText.alpha = 0f;
            thanksText.text = thanksMessage;
            gameObject.SetActive(false);
        }

        public void Show()
        {
            gameObject.SetActive(true);
            StartCoroutine(Sequence());
        }

        private IEnumerator Sequence()
        {
            yield return FadeImage(overlay, Color.clear, Color.black, fadeToBlackTime);

            if (comicImage.sprite != null)
            {
                yield return FadeImageAlpha(comicImage, 0f, 1f, comicFadeTime);
                yield return new WaitForSeconds(comicHoldTime);
                yield return FadeImageAlpha(comicImage, 1f, 0f, comicFadeTime);
            }

            yield return FadeText(thanksText, 0f, 1f, textFadeTime);
        }

        private static void SetImageAlpha(Image image, float alpha)
        {
            Color c = image.color;
            c.a = alpha;
            image.color = c;
        }

        private IEnumerator FadeImage(Image image, Color from, Color to, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                image.color = Color.Lerp(from, to, t / duration);
                yield return null;
            }
            image.color = to;
        }

        private IEnumerator FadeImageAlpha(Image image, float from, float to, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                SetImageAlpha(image, Mathf.Lerp(from, to, t / duration));
                yield return null;
            }
            SetImageAlpha(image, to);
        }

        private IEnumerator FadeText(TextMeshProUGUI text, float from, float to, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                text.alpha = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            text.alpha = to;
        }
    }
}
