using UnityEngine;
using System.Collections;
using Echoes.Audio;

namespace Echoes.Interactables
{
    public enum ButtonMode { Permanent, Timed }

    public class ToggleButton : MonoBehaviour, IInteractable
    {
        [SerializeField] private Door targetDoor;
        [SerializeField] private ButtonMode mode = ButtonMode.Permanent;
        [SerializeField] private float openDuration = 3f;

        [Header("Sprites")]
        [SerializeField] private Sprite spriteOff;
        [SerializeField] private Sprite spriteOn;

        [Header("Sonido")]
        [SerializeField] private AudioClip pressSfx;
        [Range(0f, 5f)] [SerializeField] private float pressSfxVolume = 1f;

        private SpriteRenderer _renderer;
        private Coroutine _timerCoroutine;
        private bool _isActive;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            SetVisual(false);
        }

        public void Interact()
        {
            if (targetDoor == null) return;
            // Solo en Timed: mientras está abierto por su propia cuenta cuenta atrás, pulsar otra
            // vez no debe hacer nada — si no, se podía reiniciar el temporizador y repetir el
            // sonido sin parar quedándote encima del botón. En Permanent sí se deja repulsar,
            // porque ahí pulsar de nuevo es literalmente cómo se apaga (es un interruptor).
            if (mode == ButtonMode.Timed && _timerCoroutine != null) return;

            AudioManager.Instance?.PlaySfxAt(pressSfx, transform.position, pressSfxVolume);

            if (mode == ButtonMode.Permanent)
            {
                _isActive = !_isActive;
                targetDoor.Toggle();
                SetVisual(_isActive);
            }
            else
            {
                targetDoor.Open();
                SetVisual(true);
                _timerCoroutine = StartCoroutine(CloseAfterDelay());
            }
        }

        private IEnumerator CloseAfterDelay()
        {
            yield return new WaitForSeconds(openDuration);
            targetDoor.Close();
            SetVisual(false);
            _timerCoroutine = null;
        }

        private void SetVisual(bool active)
        {
            if (_renderer == null) return;
            if (active && spriteOn != null) _renderer.sprite = spriteOn;
            else if (!active && spriteOff != null) _renderer.sprite = spriteOff;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected() => AudioManager.DrawProximityGizmo(transform.position);
#endif
    }
}
