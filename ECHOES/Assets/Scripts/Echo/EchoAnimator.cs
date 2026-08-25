using UnityEngine;

namespace Echoes.Echo
{
    [RequireComponent(typeof(Animator))]
    public class EchoAnimator : MonoBehaviour
    {
        private Animator _animator;
        private Vector2 _lastPosition;
        private Vector2 _lastDirection = Vector2.down;

        private static readonly int IsMoving = Animator.StringToHash("IsMoving");
        private static readonly int DirX = Animator.StringToHash("DirX");
        private static readonly int DirY = Animator.StringToHash("DirY");

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _lastPosition = transform.position;
        }

        private void FixedUpdate()
        {
            Vector2 delta = (Vector2)transform.position - _lastPosition;
            _lastPosition = transform.position;

            bool moving = delta.sqrMagnitude > 0.00001f;

            if (moving)
            {
                if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
                    _lastDirection = new Vector2(Mathf.Sign(delta.x), 0f);
                else
                    _lastDirection = new Vector2(0f, Mathf.Sign(delta.y));
            }

            _animator.SetBool(IsMoving, moving);
            _animator.SetFloat(DirX, _lastDirection.x);
            _animator.SetFloat(DirY, _lastDirection.y);
        }
    }
}
