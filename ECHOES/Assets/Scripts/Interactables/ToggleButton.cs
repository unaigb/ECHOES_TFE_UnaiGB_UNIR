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

        private Coroutine _timerCoroutine;

        public void Interact()
        {
            if (targetDoor == null) return;

            if (mode == ButtonMode.Permanent)
            {
                targetDoor.Toggle();
            }
            else
            {
                if (_timerCoroutine != null)
                    StopCoroutine(_timerCoroutine);

                targetDoor.Open();
                _timerCoroutine = StartCoroutine(CloseAfterDelay());
            }
        }

        private IEnumerator CloseAfterDelay()
        {
            yield return new WaitForSeconds(openDuration);
            targetDoor.Close();
            _timerCoroutine = null;
        }
    }
}
