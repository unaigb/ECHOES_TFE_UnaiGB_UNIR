using System.Collections;
using UnityEngine;
using Echoes.Interactables;

namespace Echoes.Camera
{
    public class CameraFollow : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("Follow Settings")]
        [SerializeField] private float damping = 5f;

        [Header("Zoom")]
        [SerializeField] private float defaultSize = 5f;
        [SerializeField] private float zoomedOutSize = 8f;
        [SerializeField] private float zoomSpeed = 2f;

        private UnityEngine.Camera _cam;
        private float _targetSize;
        private bool _isTransitioning;

        private float _minX = float.MinValue, _maxX = float.MaxValue;
        private float _minY = float.MinValue, _maxY = float.MaxValue;

        private void Awake()
        {
            _cam = GetComponent<UnityEngine.Camera>();
            _targetSize = defaultSize;
            _cam.orthographicSize = defaultSize;
        }

        private void LateUpdate()
        {
            if (_isTransitioning || target == null) return;

            Vector3 desired = new Vector3(target.position.x, target.position.y, transform.position.z);
            Vector3 next = Vector3.Lerp(transform.position, desired, damping * Time.deltaTime);
            transform.position = ClampToLimits(next);

            _cam.orthographicSize = Mathf.Lerp(_cam.orthographicSize, _targetSize, zoomSpeed * Time.deltaTime);
        }

        public void SetZoomedOut(bool zoomedOut)
        {
            _targetSize = zoomedOut ? zoomedOutSize : defaultSize;
        }

        public void ClearLimits()
        {
            _minX = float.MinValue; _maxX = float.MaxValue;
            _minY = float.MinValue; _maxY = float.MaxValue;
        }

        public void ApplyBarrier(ExitDirection direction, float value)
        {
            switch (direction)
            {
                case ExitDirection.Right: _maxX = value; break;
                case ExitDirection.Left:  _minX = value; break;
                case ExitDirection.Up:    _maxY = value; break;
                case ExitDirection.Down:  _minY = value; break;
            }
        }

        public IEnumerator TransitionToRoom(ExitDirection exitDirection, float duration = 0.35f)
        {
            _isTransitioning = true;
            Vector3 startPos = transform.position;

            float halfW = _cam.orthographicSize * _cam.aspect;
            float halfH = _cam.orthographicSize;
            Vector3 pan = exitDirection switch
            {
                ExitDirection.Right => new Vector3(halfW * 2f,  0f, 0f),
                ExitDirection.Left  => new Vector3(-halfW * 2f, 0f, 0f),
                ExitDirection.Up    => new Vector3(0f,  halfH * 2f, 0f),
                ExitDirection.Down  => new Vector3(0f, -halfH * 2f, 0f),
                _ => Vector3.zero
            };
            Vector3 targetPos = startPos + pan;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - (1f - t) * (1f - t); // ease-out quad
                transform.position = Vector3.Lerp(startPos, targetPos, eased);
                yield return null;
            }

            transform.position = targetPos;
            _isTransitioning = false;
        }

        private Vector3 ClampToLimits(Vector3 pos)
        {
            float halfH = _cam.orthographicSize;
            float halfW = _cam.orthographicSize * _cam.aspect;
            float x = Mathf.Clamp(pos.x, _minX + halfW, _maxX - halfW);
            float y = Mathf.Clamp(pos.y, _minY + halfH, _maxY - halfH);
            return new Vector3(x, y, pos.z);
        }
    }
}
