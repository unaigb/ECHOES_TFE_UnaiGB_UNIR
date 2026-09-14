using UnityEngine;
using Echoes.Audio;

namespace Echoes.Interactables
{
    public class Door : MonoBehaviour
    {
        [SerializeField] private bool startOpen = false;

        [Header("Sprites — parte principal")]
        [SerializeField] private Sprite spriteClosed;
        [SerializeField] private Sprite spriteOpen;

        [Header("Sprites — parte secundaria (puertas de 2 tiles)")]
        [SerializeField] private SpriteRenderer secondRenderer;
        [SerializeField] private Sprite spriteClosedSecond;
        [SerializeField] private Sprite spriteOpenSecond;

        [Header("Sonido")]
        [SerializeField] private AudioClip openSfx;
        [Range(0f, 5f)] [SerializeField] private float openSfxVolume = 1f;
        [SerializeField] private AudioClip closeSfx;
        [Range(0f, 5f)] [SerializeField] private float closeSfxVolume = 1f;

        private bool _isOpen;
        private Collider2D _collider;
        private SpriteRenderer _renderer;

        private void Awake()
        {
            _collider = GetComponent<Collider2D>();
            _renderer = GetComponent<SpriteRenderer>();
            SetState(startOpen, playSound: false);
        }

        public void Open() => SetState(true, playSound: true);
        public void Close() => SetState(false, playSound: true);
        public void Toggle() => SetState(!_isOpen, playSound: true);

        // Para cuando otro script decide, al cargar, que el startOpen serializado no aplica de
        // verdad (p. ej. AutoCloseDoor, si el jugador ya reapareció al otro lado) — sin sonido,
        // igual que el propio Awake().
        public void SetInitialState(bool open) => SetState(open, playSound: false);

        private void SetState(bool open, bool playSound)
        {
            bool changed = _isOpen != open;
            _isOpen = open;
            _collider.enabled = !open;

            if (playSound && changed)
            {
                if (open) AudioManager.Instance?.PlaySfxAt(openSfx, transform.position, openSfxVolume);
                else AudioManager.Instance?.PlaySfxAt(closeSfx, transform.position, closeSfxVolume);
            }

            if (_renderer != null)
            {
                Sprite target = open ? spriteOpen : spriteClosed;
                if (target != null)
                    _renderer.sprite = target;
                else
                    _renderer.color = open ? new Color(1f, 1f, 1f, 0.3f) : Color.white;
            }

            if (secondRenderer != null)
            {
                Sprite targetSecond = open ? spriteOpenSecond : spriteClosedSecond;
                if (targetSecond != null)
                    secondRenderer.sprite = targetSecond;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected() => AudioManager.DrawProximityGizmo(transform.position);
#endif
    }
}
