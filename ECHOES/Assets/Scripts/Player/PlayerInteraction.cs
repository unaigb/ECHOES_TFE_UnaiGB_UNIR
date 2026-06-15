using UnityEngine;
using UnityEngine.InputSystem;
using Echoes.Interactables;

namespace Echoes.Player
{
    public class PlayerInteraction : MonoBehaviour
    {
        [SerializeField] private float interactRadius = 0.8f;
        [SerializeField] private LayerMask interactableLayer;

        private InputAction _interactAction;

        private void Awake()
        {
            _interactAction = InputSystem.actions.FindAction("Player/Interact");
        }

        private void OnEnable()
        {
            _interactAction.performed += OnInteract;
        }

        private void OnDisable()
        {
            _interactAction.performed -= OnInteract;
        }

        private void OnInteract(InputAction.CallbackContext ctx)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactRadius, interactableLayer);
            foreach (var hit in hits)
            {
                if (hit.TryGetComponent<IInteractable>(out var interactable))
                {
                    interactable.Interact();
                    break; // solo interactúa con el más cercano
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactRadius);
        }
    }
}
