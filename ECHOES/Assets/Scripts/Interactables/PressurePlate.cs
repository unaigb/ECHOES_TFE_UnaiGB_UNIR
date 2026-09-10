using UnityEngine;
using System.Collections;

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

        private int _activatorCount = 0;
        private Vector3 _originalScale;
        private Coroutine _visualRoutine;

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
            if (_activatorCount == 1)
            {
                foreach (var door in targetDoors) door?.Open();
                SetPressedVisual(true);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsValidActivator(other)) return;
            _activatorCount--;
            if (_activatorCount <= 0)
            {
                _activatorCount = 0;
                foreach (var door in targetDoors) door?.Close();
                SetPressedVisual(false);
            }
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
    }
}
