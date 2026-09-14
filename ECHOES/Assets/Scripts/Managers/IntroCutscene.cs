using UnityEngine;
using System.Collections;
using Echoes.Player;
using Echoes.Camera;
using Echoes.UI;
using Echoes.Interactables;
using Echoes.Dialogue;
using Echoes.Saving;

namespace Echoes.Managers
{
    public class IntroCutscene : MonoBehaviour
    {
        [SerializeField] private PlayerInputHandler playerInput;
        [SerializeField] private PlayerAnimator playerAnimator;
        [SerializeField] private CameraFollow cameraFollow;
        [SerializeField] private LetterboxBars letterbox;
        [SerializeField] private Door startDoor;
        [Tooltip("Opcional: primer diálogo de Ir1s, se lanza justo al terminar la cinemática de entrada.")]
        [SerializeField] private DialogueSequence firstDialogue;
        [Tooltip("CanvasGroup del HUD de juego (RecordingHUD, indicador de sala, etc.) — se hace aparecer con un fundido al terminar el paseo. GameEntryPoint ya lo pone a 0 antes del aviso de autoguardado.")]
        [SerializeField] private CanvasGroup hudCanvasGroup;
        [SerializeField] private float hudFadeInTime = 0.6f;
        [Tooltip("El mismo indicador de sala (\"LocationBox\") que usa PauseMenu — no cuelga del CanvasGroup del HUD, así que se reactiva aparte, a la vez que el HUD, al terminar el paseo.")]
        [SerializeField] private GameObject locationBox;

        [Header("Andar")]
        [SerializeField] private Vector2 walkDirection = Vector2.up;
        [SerializeField] private float walkDuration = 2f;

        [Header("Cámara")]
        [SerializeField] private float cutsceneCameraSize = 3f;
        [Tooltip("Damping durante la cinemática — más bajo que el normal porque con la cámara tan cerca, el retraso habitual se nota mucho más. Se restaura solo al terminar.")]
        [SerializeField] private float cutsceneDamping = 0f;

        private float _normalDamping;

        // Llamado por GameEntryPoint ANTES del aviso de autoguardado: pone en marcha el encuadre
        // de cámara de la cinemática (zoom + posición congelada, sin límites de sala). Así, para
        // cuando se retire el aviso, la cámara lleva ya toda la duración del fundido + espera
        // acercándose a su sitio y no se ve ningún ajuste — el paseo puede arrancar de inmediato.
        public void PrepareForReveal()
        {
            playerInput.InputEnabled = false;
            playerAnimator.SetFacingDirection(walkDirection);

            _normalDamping = cameraFollow.GetDamping();
            cameraFollow.SetDamping(cutsceneDamping);
            cameraFollow.ClearLimits();
            cameraFollow.SetZoomLocked(true);
            cameraFollow.SetCustomSize(cutsceneCameraSize);
        }

        // Llamado por GameEntryPoint justo cuando el aviso de autoguardado termina de
        // desvanecerse: como darle a "play", sin más espera — el encuadre ya está listo gracias
        // a PrepareForReveal().
        public void Begin()
        {
            StartCoroutine(Play());
        }

        private IEnumerator Play()
        {
            playerInput.SetMovementOverride(walkDirection);
            yield return new WaitForSeconds(walkDuration);
            playerInput.SetMovementOverride(null);

            startDoor.Close();
            yield return new WaitForSeconds(0.3f);

            cameraFollow.SetZoomLocked(false);
            cameraFollow.SetZoomedOut(false); // vuelve al tamaño normal de juego
            cameraFollow.SetDamping(_normalDamping);

            if (locationBox != null) locationBox.SetActive(true);
            yield return StartCoroutine(letterbox.Hide());
            yield return StartCoroutine(FadeCanvasGroup(hudCanvasGroup, 0f, 1f, hudFadeInTime));

            playerInput.InputEnabled = true;
            GameFlow.StartTimerIfNeeded();

            if (firstDialogue != null && DialogueManager.Instance != null)
                DialogueManager.Instance.Play(firstDialogue);
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
