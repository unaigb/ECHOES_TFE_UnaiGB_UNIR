using UnityEngine;

namespace Echoes.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PlayerInputHandler))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 4f;

        private Rigidbody2D _rb;
        private PlayerInputHandler _input;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _input = GetComponent<PlayerInputHandler>();

            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
        }

        private void FixedUpdate()
        {
            Vector2 velocity = _input.MoveInput.normalized * moveSpeed;
            _rb.linearVelocity = velocity;
        }
    }
}
