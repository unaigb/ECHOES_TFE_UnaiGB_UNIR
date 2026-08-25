using UnityEngine;
using System.Collections;

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

            if (mode == ButtonMode.Permanent)
            {
                _isActive = !_isActive;
                targetDoor.Toggle();
                SetVisual(_isActive);
            }
            else
            {
                if (_timerCoroutine != null)
                    StopCoroutine(_timerCoroutine);

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
    }
}
