using UnityEngine;

namespace Echoes.Interactables
{
    public class Door : MonoBehaviour
    {
        [SerializeField] private bool startOpen = false;

        private bool _isOpen;
        private Collider2D _collider;
        private SpriteRenderer _renderer;

        private void Awake()
        {
            _collider = GetComponent<Collider2D>();
            _renderer = GetComponent<SpriteRenderer>();
            SetState(startOpen);
        }

        public void Open() => SetState(true);
        public void Close() => SetState(false);
        public void Toggle() => SetState(!_isOpen);

        private void SetState(bool open)
        {
            _isOpen = open;
            _collider.enabled = !open;
            if (_renderer != null)
                _renderer.color = open ? new Color(1f, 1f, 1f, 0.3f) : Color.white;
        }
    }
}
