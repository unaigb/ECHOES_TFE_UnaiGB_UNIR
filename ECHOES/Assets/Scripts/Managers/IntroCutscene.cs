using UnityEngine;
using System.Collections;
using Echoes.Player;
using Echoes.Camera;
using Echoes.UI;
using Echoes.Interactables;

namespace Echoes.Managers
{
    public class IntroCutscene : MonoBehaviour
    {
        [SerializeField] private PlayerInputHandler playerInput;
        [SerializeField] private PlayerAnimator playerAnimator;
        [SerializeField] private CameraFollow cameraFollow;
        [SerializeField] private LetterboxBars letterbox;
        [SerializeField] private Door startDoor;
        [Tooltip("CanvasGroup del HUD de juego (RecordingHUD, indicador de sala, etc.) — se oculta durante la cinemática y aparece con un fundido al terminar.")]
        [SerializeField] private CanvasGroup hudCanvasGroup;
        [SerializeField] private float hudFadeInTime = 0.6f;

        [Header("Andar")]
        [SerializeField] private Vector2 walkDirection = Vector2.up;
        [SerializeField] private float walkDuration = 2f;

        [Header("Cámara")]
        [SerializeField] private float cutsceneCameraSize = 3f;
        [Tooltip("Damping durante la cinemática — más bajo que el normal porque con la cámara tan cerca, el retraso habitual se nota mucho más. Se restaura solo al terminar.")]
        [SerializeField] private float cutsceneDamping = 0f;

        private void Start()
        {
            playerInput.InputEnabled = false;
            playerAnimator.SetFacingDirection(walkDirection);
            hudCanvasGroup.alpha = 0f;
            StartCoroutine(Play());
        }

        private IEnumerator Play()
        {
            float normalDamping = cameraFollow.GetDamping();
            cameraFollow.SetDamping(cutsceneDamping);

            // Sin límites durante la cinemática: es un recorrido controlado, no hay riesgo
            // de que la cámara muestre algo no deseado. El cruce de la puerta al final
            // vuelve a aplicar los bounds reales de Sala 01 a través del sistema normal.
            cameraFollow.ClearLimits();
            // Bloquea el zoom: el cruce de la puerta dispara el sistema normal de salas (que
            // llama a SetZoomedOut internamente) y sin esto pisaría este tamaño a mitad de paseo.
            cameraFollow.SetZoomLocked(true);
            cameraFollow.SetCustomSize(cutsceneCameraSize);

            yield return new WaitForSeconds(0.3f);

            playerInput.SetMovementOverride(walkDirection);
            yield return new WaitForSeconds(walkDuration);
            playerInput.SetMovementOverride(null);

            startDoor.Close();
            yield return new WaitForSeconds(0.3f);

            cameraFollow.SetZoomLocked(false);
            cameraFollow.SetZoomedOut(false); // vuelve al tamaño normal de juego
            cameraFollow.SetDamping(normalDamping);
            yield return StartCoroutine(letterbox.Hide());
            yield return StartCoroutine(FadeCanvasGroup(hudCanvasGroup, 0f, 1f, hudFadeInTime));

            playerInput.InputEnabled = true;

            // TODO (Fase 4): disparar aquí el primer diálogo de la IA una vez exista el sistema.
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
