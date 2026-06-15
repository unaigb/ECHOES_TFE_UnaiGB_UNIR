using UnityEngine;

namespace Echoes.Interactables
{
    [RequireComponent(typeof(Collider2D))]
    public class PressurePlate : MonoBehaviour
    {
        [SerializeField] private Door targetDoor;

        private int _activatorCount = 0;

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player") && !other.CompareTag("Echo")) return;
            _activatorCount++;
            if (_activatorCount == 1)
                targetDoor?.Open();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag("Player") && !other.CompareTag("Echo")) return;
            _activatorCount--;
            if (_activatorCount <= 0)
            {
                _activatorCount = 0;
                targetDoor?.Close();
            }
        }
    }
}
