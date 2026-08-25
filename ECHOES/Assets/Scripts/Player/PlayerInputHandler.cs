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

        private void Update()
        {
            MoveInput = (InputEnabled && _moveAction != null) ? _moveAction.ReadValue<Vector2>() : Vector2.zero;
        }
    }
}
