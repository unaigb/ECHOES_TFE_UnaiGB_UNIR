using UnityEngine;
using Echoes.Echo;

namespace Echoes.Player
{
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimator : MonoBehaviour
    {
        [SerializeField] private PlayerInputHandler input;
        [SerializeField] private RuntimeAnimatorController lockedController;
        [SerializeField] private RuntimeAnimatorController unlockedController;

        private Animator _animator;
        private Vector2 _lastDirection = Vector2.down;

        private static readonly int IsMoving = Animator.StringToHash("IsMoving");
        private static readonly int DirX = Animator.StringToHash("DirX");
        private static readonly int DirY = Animator.StringToHash("DirY");

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        private void Update()
        {
            Vector2 move = input.MoveInput;
            bool moving = move.sqrMagnitude > 0.01f;

            if (moving)
            {
                // Solo actualiza la dirección dominante (4 direcciones)
                if (Mathf.Abs(move.x) >= Mathf.Abs(move.y))
                    _lastDirection = new Vector2(Mathf.Sign(move.x), 0f);
                else
                    _lastDirection = new Vector2(0f, Mathf.Sign(move.y));
            }

            _animator.SetBool(IsMoving, moving);
            _animator.SetFloat(DirX, _lastDirection.x);
            _animator.SetFloat(DirY, _lastDirection.y);
        }

        public void SetEcoUnlocked(bool unlocked)
        {
            _animator.runtimeAnimatorController = unlocked ? unlockedController : lockedController;
        }
    }
}
