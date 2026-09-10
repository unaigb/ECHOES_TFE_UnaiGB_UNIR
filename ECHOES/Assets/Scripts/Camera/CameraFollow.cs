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
        private Transform _defaultTarget;

        private float _minX = float.MinValue, _maxX = float.MaxValue;
        private float _minY = float.MinValue, _maxY = float.MaxValue;

        private void Awake()
        {
            _cam = GetComponent<UnityEngine.Camera>();
            _targetSize = defaultSize;
            _cam.orthographicSize = defaultSize;
            _defaultTarget = target;
        }

        public void SetSpectateTarget(Transform spectateTarget)
        {
            target = spectateTarget != null ? spectateTarget : _defaultTarget;
        }

        public float GetDamping() => damping;

        public void SetDamping(float value)
        {
            damping = value;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            // Clampa el OBJETIVO, no la posición ya interpolada: así, si los bounds se ponen más
            // restrictivos de golpe (p.ej. justo al cruzar una puerta), la cámara viaja hasta el
            // punto válido con el damping normal en vez de teletransportarse ahí en un frame.
            Vector3 desired = new Vector3(target.position.x, target.position.y, transform.position.z);
            Vector3 clampedDesired = ClampToLimits(desired);
            transform.position = Vector3.Lerp(transform.position, clampedDesired, damping * Time.deltaTime);

            _cam.orthographicSize = Mathf.Lerp(_cam.orthographicSize, _targetSize, zoomSpeed * Time.deltaTime);
        }

        private bool _zoomLocked;

        // Para cinemáticas: mientras está bloqueado, SetZoomedOut() (el que dispara el cruce
        // normal de una sala) no toca el tamaño de cámara — solo SetCustomSize() manda.
        public void SetZoomLocked(bool locked) => _zoomLocked = locked;

        public void SetZoomedOut(bool zoomedOut)
        {
            if (_zoomLocked) return;
            _targetSize = zoomedOut ? zoomedOutSize : defaultSize;
        }

        // Para cinemáticas: cualquier tamaño arbitrario, no solo default/zoomed-out.
        // Para volver al tamaño normal de juego, usa SetZoomedOut(false) (usa defaultSize).
        public void SetCustomSize(float size)
        {
            _targetSize = size;
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
