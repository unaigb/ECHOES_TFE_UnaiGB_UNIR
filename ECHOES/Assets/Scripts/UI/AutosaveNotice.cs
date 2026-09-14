using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace Echoes.UI
{
    // Pantalla de revelado: TitleScreen (o el fundido a negro de DetectionScreen) ya deja la
    // pantalla en negro antes de cargar esta escena, así que aquí el fondo nace directamente
    // opaco. El fundido de vuelta (fondo + texto) pasa SIEMPRE al entrar a la partida — con
    // texto solo la primera vez de la sesión (GameEntryPoint decide showText); en una
    // recarga por detección o "Restart Room" es solo el fundido, sin mensaje, para que el
    // jugador no reaparezca en su sitio sin más y el input no se active hasta que se ve algo.
    public class AutosaveNotice : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private string message = "Progress saves automatically whenever you reach a new room.";
        [SerializeField] private float textFadeInTime = 0.5f;
        [SerializeField] private float holdTime = 1.6f;
        [Tooltip("Espera cuando NO se muestra texto (recarga por detección o Restart Room) — un corte a negro instantáneo se siente peor que un pequeño respiro.")]
        [SerializeField] private float blackoutHoldTime = 0.5f;
        [SerializeField] private float fadeOutTime = 0.6f;
        [Tooltip("Duración del fundido A negro cuando algo (Restart Room) lo pide antes de recargar la escena — separado de fadeOutTime porque es el sentido contrario.")]
        [SerializeField] private float fadeInTime = 0.4f;

        // PauseMenu lo consulta para no dejar pausar mientras la pantalla está en negro — si no,
        // con todo a negro es fácil pausar sin querer y no queda claro qué ha pasado.
        public bool IsPlaying { get; private set; }

        private void Awake()
        {
            // Invisible por defecto: si esta sesión no toca mostrar el aviso (p. ej. un reinicio
            // por detección, que no pasa por Title y por tanto no tiene el fundido previo), no
            // debe tapar nada.
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
            if (messageText != null)
            {
                messageText.text = message;
                messageText.alpha = 0f;
            }
        }

        public void Play(bool showText, Action onComplete)
        {
            StartCoroutine(Sequence(showText, onComplete));
        }

        // Para fundir A negro ANTES de recargar la escena (Restart Room desde Pausa): al volver
        // a cargar, esta misma pantalla ya nace opaca y Play() se encarga del resto — mismo
        // truco de continuidad que el fundido de Title antes de entrar a la partida.
        public void FadeToBlack(Action onComplete)
        {
            StartCoroutine(FadeInSequence(onComplete));
        }

        private IEnumerator FadeInSequence(Action onComplete)
        {
            group.blocksRaycasts = true;
            // Si el aviso con texto ya se mostró esta sesión (p. ej. al arrancar la partida),
            // messageText se quedó en su propio alpha 1 — solo se desvaneció el group entero.
            // Sin este reset, al volver a subir group.alpha aquí (Restart Room) el texto viejo
            // reaparecía arrastrado de esa vez anterior.
            if (messageText != null) messageText.alpha = 0f;
            yield return FadeGroupAlpha(group.alpha, 1f, fadeInTime);
            onComplete?.Invoke();
        }

        private IEnumerator Sequence(bool showText, Action onComplete)
        {
            IsPlaying = true;
            // El fondo ya está negro por el fundido previo (Title, o el de DetectionScreen antes
            // de recargar): aquí se deja opaco de golpe, sin animar.
            group.alpha = 1f;
            group.blocksRaycasts = true;
            // Reseteado explícito: en una recarga anterior con showText=true, el texto se quedó
            // en su propio alpha 1 (solo se desvaneció el group entero) — sin esto, reaparecería
            // ya visible desde el primer frame de una recarga sin mensaje.
            if (messageText != null) messageText.alpha = 0f;

            if (showText)
            {
                yield return FadeTextAlpha(0f, 1f, textFadeInTime);
                yield return new WaitForSeconds(holdTime);
            }
            else
            {
                yield return new WaitForSeconds(blackoutHoldTime);
            }

            yield return FadeGroupAlpha(1f, 0f, fadeOutTime);

            group.blocksRaycasts = false;
            IsPlaying = false;
            onComplete?.Invoke();
        }

        private IEnumerator FadeTextAlpha(float from, float to, float duration)
        {
            if (messageText == null) yield break;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                messageText.alpha = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            messageText.alpha = to;
        }

        private IEnumerator FadeGroupAlpha(float from, float to, float duration)
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
