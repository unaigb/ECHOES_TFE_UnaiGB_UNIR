using UnityEngine;
using System.Collections;
using Echoes.Audio;

namespace Echoes.Interactables
{
    [RequireComponent(typeof(Collider2D))]
    public class PressurePlate : MonoBehaviour
    {
        [SerializeField] private Door[] targetDoors;
        [Tooltip("Si se marca, solo una MovableBox con Is Big activa esta placa — el jugador, el eco y las cajas normales quedan excluidos.")]
        [SerializeField] private bool requiresBigBox;

        [Header("Visual")]
        [SerializeField] private float pressedScaleY = 0.7f;
        [SerializeField] private float visualTransitionTime = 0.08f;

        [Header("Sonido")]
        [SerializeField] private AudioClip pressSfx;
        [Range(0f, 5f)] [SerializeField] private float pressSfxVolume = 1f;
        [SerializeField] private AudioClip releaseSfx;
        [Range(0f, 5f)] [SerializeField] private float releaseSfxVolume = 1f;
        [Tooltip("Pequeño margen antes de soltar la placa de verdad — absorbe el tembleque físico cuando la caja queda justo al borde del trigger y entra/sale del collider varias veces por frame de física.")]
        [SerializeField] private float releaseGrace = 0.15f;

        private int _activatorCount = 0;
        private Vector3 _originalScale;
        private Coroutine _visualRoutine;
        private Coroutine _releaseRoutine;

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
            _originalScale = transform.localScale;
        }

        private bool IsValidActivator(Collider2D other)
        {
            if (requiresBigBox)
            {
                MovableBox box = other.GetComponent<MovableBox>();
                return box != null && box.IsBig;
            }
            return other.CompareTag("PlayerFeet") || other.CompareTag("Echo") || other.CompareTag("Box");
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsValidActivator(other)) return;

            _activatorCount++;
            // Cancela una suelta pendiente: si esto es el mismo objeto rebotando en el borde del
            // trigger, la placa nunca llega a soltarse de verdad.
            if (_releaseRoutine != null) { StopCoroutine(_releaseRoutine); _releaseRoutine = null; }

            if (_activatorCount == 1)
            {
                foreach (var door in targetDoors) door?.Open();
                SetPressedVisual(true);
                AudioManager.Instance?.PlaySfxAt(pressSfx, transform.position, pressSfxVolume);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsValidActivator(other)) return;

            _activatorCount--;
            if (_activatorCount <= 0)
            {
                _activatorCount = 0;
                if (_releaseRoutine == null)
                    _releaseRoutine = StartCoroutine(ReleaseAfterGrace());
            }
        }

        // Espera un momento antes de soltar de verdad — si algo vuelve a entrar mientras tanto
        // (el mismo tembleque físico que causó la salida), OnTriggerEnter2D ya habrá cancelado
        // esta corrutina, así que ni siquiera llega a comprobar nada.
        private IEnumerator ReleaseAfterGrace()
        {
            yield return new WaitForSeconds(releaseGrace);
            _releaseRoutine = null;

            foreach (var door in targetDoors) door?.Close();
            SetPressedVisual(false);
            AudioManager.Instance?.PlaySfxAt(releaseSfx, transform.position, releaseSfxVolume);
        }

        private void SetPressedVisual(bool pressed)
        {
            if (_visualRoutine != null) StopCoroutine(_visualRoutine);
            Vector3 target = pressed
                ? new Vector3(_originalScale.x, _originalScale.y * pressedScaleY, _originalScale.z)
                : _originalScale;
            _visualRoutine = StartCoroutine(ScaleTo(target));
        }

        private IEnumerator ScaleTo(Vector3 target)
        {
            Vector3 start = transform.localScale;
            float elapsed = 0f;
            while (elapsed < visualTransitionTime)
            {
                elapsed += Time.deltaTime;
                transform.localScale = Vector3.Lerp(start, target, elapsed / visualTransitionTime);
                yield return null;
            }
            transform.localScale = target;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected() => AudioManager.DrawProximityGizmo(transform.position);
#endif
    }
}
