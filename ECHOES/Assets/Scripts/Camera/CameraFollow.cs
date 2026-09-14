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

        // Para reanudar una partida sin pasar por la cinemática (Continue, reinicio por
        // detección, salto de sala de depuración): coloca la cámara YA en su posición y tamaño
        // objetivo, sin el Lerp normal — si no, se ve un barrido desde donde estuviera la cámara
        // al cargar la escena hasta el jugador, y el zoom (p.ej. el de la Sala 04) tarda un rato
        // en alcanzar su tamaño en vez de aparecer ya aplicado.
        // 'position' se pasa explícito (no se lee de 'target') para evitar depender de que el
        // Transform del jugador ya refleje una teletransportación hecha el mismo frame.
        public void SnapToCurrentState(Vector3 position)
        {
            transform.position = new Vector3(position.x, position.y, transform.position.z);
            _cam.orthographicSize = _targetSize;
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

            // Si la sala es más pequeña que la vista de la cámara en algún eje (zoom grande +
            // spawn pegado a un límite, p. ej. el bound sur de la Sala 03), min queda por
            // encima de max y Mathf.Clamp con el rango invertido da un resultado errático según
            // de qué lado caiga pos — se centra en ese eje en vez de dejar que "colapse".
            float minX = _minX + halfW, maxX = _maxX - halfW;
            float minY = _minY + halfH, maxY = _maxY - halfH;
            float x = minX <= maxX ? Mathf.Clamp(pos.x, minX, maxX) : (_minX + _maxX) * 0.5f;
            float y = minY <= maxY ? Mathf.Clamp(pos.y, minY, maxY) : (_minY + _maxY) * 0.5f;
            return new Vector3(x, y, pos.z);
        }
    }
}
