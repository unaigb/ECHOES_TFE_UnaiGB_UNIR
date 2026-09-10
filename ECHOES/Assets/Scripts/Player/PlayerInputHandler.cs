using UnityEngine;
using UnityEngine.InputSystem;

namespace Echoes.Player
{
    public class PlayerInputHandler : MonoBehaviour
    {
        public Vector2 MoveInput { get; private set; }

        private InputAction _moveAction;

        private void Awake()
        {
            _moveAction = InputSystem.actions.FindAction("Player/Move");
        }

        public bool InputEnabled { get; set; } = true;

        private Vector2? _movementOverride;

        // Para cinemáticas: simula que el jugador pulsa una dirección, sin input real.
        // Tanto PlayerController como PlayerAnimator leen MoveInput, así que ambos reaccionan solos.
        public void SetMovementOverride(Vector2? move) => _movementOverride = move;

        private void Update()
        {
            if (_movementOverride.HasValue)
            {
                MoveInput = _movementOverride.Value;
                return;
            }
            MoveInput = (InputEnabled && _moveAction != null) ? _moveAction.ReadValue<Vector2>() : Vector2.zero;
        }
    }
}
