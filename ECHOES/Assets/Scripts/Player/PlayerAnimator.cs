using UnityEngine;
using System.Collections;
using Echoes.Echo;
using Echoes.Interactables;

namespace Echoes.Player
{
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimator : MonoBehaviour
    {
        [SerializeField] private PlayerInputHandler input;
        [SerializeField] private PlayerInteraction interaction;
        [SerializeField] private RuntimeAnimatorController lockedController;
        [SerializeField] private RuntimeAnimatorController unlockedController;

        [Header("Gesto de interactuar (placeholder sin arte dedicado)")]
        [SerializeField] private float interactPulseScale = 1.15f;
        [SerializeField] private float interactPulseTime = 0.12f;

        [Header("Idle respirando")]
        [Tooltip("En píxeles (no unidades de mundo) — cuántos píxeles sube y baja como máximo.")]
        [SerializeField] private int breathAmplitudePixels = 1;
        [SerializeField] private float breathSpeed = 2f;
        [Tooltip("Pixels Per Unit del sprite del jugador (18 en el proyecto actual). El offset se cuantiza a múltiplos de 1 píxel para evitar el parpadeo de subpíxel.")]
        [SerializeField] private float pixelsPerUnit = 18f;

        private Animator _animator;
        private Vector2 _lastDirection = Vector2.down;
        private Vector3 _baseScale;
        private Vector3 _baseLocalPosition;
        private Coroutine _interactPulse;
        private bool _isMoving;

        private static readonly int IsMoving = Animator.StringToHash("IsMoving");
        private static readonly int DirX = Animator.StringToHash("DirX");
        private static readonly int DirY = Animator.StringToHash("DirY");

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _baseScale = transform.localScale;
            _baseLocalPosition = transform.localPosition;
        }

        private void OnEnable()
        {
            if (interaction != null) interaction.OnInteracted += HandleInteracted;
        }

        private void OnDisable()
        {
            if (interaction != null) interaction.OnInteracted -= HandleInteracted;
        }

        private void HandleInteracted(IInteractable _)
        {
            if (_interactPulse != null) StopCoroutine(_interactPulse);
            _interactPulse = StartCoroutine(InteractPulse());
        }

        private IEnumerator InteractPulse()
        {
            Vector3 peak = _baseScale * interactPulseScale;
            float half = interactPulseTime * 0.5f;

            float t = 0f;
            while (t < half)
            {
                t += Time.deltaTime;
                transform.localScale = Vector3.Lerp(_baseScale, peak, t / half);
                yield return null;
            }
            t = 0f;
            while (t < half)
            {
                t += Time.deltaTime;
                transform.localScale = Vector3.Lerp(peak, _baseScale, t / half);
                yield return null;
            }
            transform.localScale = _baseScale;
        }

        private void Update()
        {
            Vector2 move = input.MoveInput;
            _isMoving = move.sqrMagnitude > 0.01f;

            if (_isMoving)
            {
                // Solo actualiza la dirección dominante (4 direcciones)
                if (Mathf.Abs(move.x) >= Mathf.Abs(move.y))
                    _lastDirection = new Vector2(Mathf.Sign(move.x), 0f);
                else
                    _lastDirection = new Vector2(0f, Mathf.Sign(move.y));
            }

            _animator.SetBool(IsMoving, _isMoving);
            _animator.SetFloat(DirX, _lastDirection.x);
            _animator.SetFloat(DirY, _lastDirection.y);
        }

        // Se aplica después de que el Animator evalúe el frame, para que el bob no
        // compita con él por la posición (Write Defaults puede reescribirla si se hace en Update).
        private void LateUpdate()
        {
            float pixelSize = 1f / pixelsPerUnit;
            float amplitude = breathAmplitudePixels * pixelSize;

            float raw = _isMoving ? 0f : Mathf.Sin(Time.time * breathSpeed) * amplitude;
            float snapped = Mathf.Round(raw / pixelSize) * pixelSize;

            transform.localPosition = _baseLocalPosition + new Vector3(0f, snapped, 0f);
        }

        // Para cinemáticas: fija el sprite de idle a una dirección concreta antes de que el
        // jugador se mueva por primera vez (por defecto sale mirando hacia abajo).
        public void SetFacingDirection(Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.01f) return;
            _lastDirection = Mathf.Abs(direction.x) >= Mathf.Abs(direction.y)
                ? new Vector2(Mathf.Sign(direction.x), 0f)
                : new Vector2(0f, Mathf.Sign(direction.y));
        }

        public void SetEcoUnlocked(bool unlocked)
        {
            _animator.runtimeAnimatorController = unlocked ? unlockedController : lockedController;
        }
    }
}
