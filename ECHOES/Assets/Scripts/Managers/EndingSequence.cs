using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using Echoes.Saving;
using Echoes.Audio;

namespace Echoes.Managers
{
    public class EndingSequence : MonoBehaviour
    {
        [SerializeField] private Image overlay;

        [Header("Cómic de cierre (opcional)")]
        [Tooltip("Los trozos de la PÁGINA de cómic, ya colocados cada uno en su sitio final (su propio RectTransform: panel 1 a media izquierda entera, 2 arriba-derecha, 3 abajo-derecha, \"To Be Continued\" solapando la esquina — o el layout que quieras). El código solo va subiendo el alpha de cada uno en este orden, SIN ocultar los anteriores, hasta dejar la página completa a la vista. Deja huecos vacíos si usas menos de los que tengas en la lista.")]
        [SerializeField] private CanvasGroup[] comicPanels;
        [SerializeField] private float panelFadeTime = 0.5f;
        [Tooltip("Pausa entre que un panel termina de aparecer y empieza el siguiente.")]
        [SerializeField] private float panelDelayBetween = 0.5f;
        [Tooltip("Cuánto se queda la página completa (los 4 elementos ya visibles) antes de fundir a negro.")]
        [SerializeField] private float pageHoldTime = 3f;

        [Header("Agradecimiento")]
        [SerializeField] private TextMeshProUGUI thanksText;
        [Tooltip("CanvasGroup que envuelve el texto de agradecimiento y todo lo que cuelgue de él (subtítulo, logo...). El fade-in se hace sobre este grupo, no solo sobre el TMP, para que los hijos también aparezcan progresivamente.")]
        [SerializeField] private CanvasGroup thanksGroup;
        [SerializeField] private string thanksMessage = "Gracias por jugar la demo";
        [Tooltip("Opcional: texto del tiempo de partida, normalmente debajo del agradecimiento — hijo de thanksGroup para que aparezca con el mismo fundido. Déjalo vacío si no lo quieres mostrar.")]
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private string timerLabelFormat = "Time: {0}";

        [Header("Tiempos (segundos)")]
        [SerializeField] private float fadeToBlackTime = 1f;
        [SerializeField] private float textFadeTime = 1f;
        [Tooltip("Segundos desde que aparece el mensaje de agradecimiento hasta volver solo al Título.")]
        [SerializeField] private float returnToTitleDelay = 10f;
        [Tooltip("Nombre exacto de la escena de título, tal cual aparece en Build Settings.")]
        [SerializeField] private string titleSceneName = "Title";

        private void Awake()
        {
            overlay.color = Color.clear;
            if (comicPanels != null)
                foreach (var panel in comicPanels)
                    if (panel != null) panel.alpha = 0f;
            thanksGroup.alpha = 0f;
            thanksText.text = thanksMessage;
            gameObject.SetActive(false);
        }

        public void Show()
        {
            // Se para aquí, no en GameEndTrigger: este método se llama exactamente en el
            // instante en que termina el diálogo de cierre (o de inmediato si no hay diálogo),
            // que es justo el momento pedido para detener el cronómetro.
            GameFlow.StopTimerIfNeeded();
            if (timerText != null)
                timerText.text = string.Format(timerLabelFormat, FormatTime(GameFlow.ElapsedPlaytime));

            gameObject.SetActive(true);
            StartCoroutine(Sequence());
        }

        private static string FormatTime(float seconds)
        {
            int total = Mathf.Max(0, Mathf.RoundToInt(seconds));
            return $"{total / 60:00}:{total % 60:00}";
        }

        private IEnumerator Sequence()
        {
            AudioManager.Instance?.StopMusic(fadeToBlackTime);
            yield return FadeImage(overlay, Color.clear, Color.black, fadeToBlackTime);

            yield return PlayComicPanels();

            yield return FadeCanvasGroup(thanksGroup, 0f, 1f, textFadeTime);

            yield return new WaitForSeconds(returnToTitleDelay);
            GameFlow.LeavingScene = true;
            SceneManager.LoadScene(titleSceneName);
        }

        // Revela los trozos de la página de cómic uno a uno, SIN ocultar los anteriores (cada
        // uno ya está en su sitio final de la página) — al terminar, se queda la página completa
        // un rato y luego se funde todo junto a negro, dejando el mismo overlay negro de siempre
        // para el agradecimiento.
        private IEnumerator PlayComicPanels()
        {
            if (comicPanels == null) yield break;

            bool any = false;
            foreach (var panel in comicPanels) if (panel != null) any = true;
            if (!any) yield break; // ningún panel asignado: cómic saltado entero

            foreach (var panel in comicPanels)
            {
                if (panel == null) continue;
                yield return FadeCanvasGroup(panel, 0f, 1f, panelFadeTime);
                yield return new WaitForSeconds(panelDelayBetween);
            }

            yield return new WaitForSeconds(pageHoldTime);
            yield return FadeAllPanelsOut(panelFadeTime);
        }

        private IEnumerator FadeAllPanelsOut(float duration)
        {
            float[] start = new float[comicPanels.Length];
            for (int i = 0; i < comicPanels.Length; i++)
                start[i] = comicPanels[i] != null ? comicPanels[i].alpha : 0f;

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = duration > 0f ? t / duration : 1f;
                for (int i = 0; i < comicPanels.Length; i++)
                    if (comicPanels[i] != null) comicPanels[i].alpha = Mathf.Lerp(start[i], 0f, k);
                yield return null;
            }
            for (int i = 0; i < comicPanels.Length; i++)
                if (comicPanels[i] != null) comicPanels[i].alpha = 0f;
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

        private IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                group.alpha = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            group.alpha = to;
        }
    }
}
