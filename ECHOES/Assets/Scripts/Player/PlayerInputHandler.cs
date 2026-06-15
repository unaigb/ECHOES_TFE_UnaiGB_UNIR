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

        private void Update()
        {
            MoveInput = _moveAction != null ? _moveAction.ReadValue<Vector2>() : Vector2.zero;
        }
    }
}
