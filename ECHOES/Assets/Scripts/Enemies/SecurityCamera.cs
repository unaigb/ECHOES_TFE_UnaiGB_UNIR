using UnityEngine;
using Echoes.Managers;

namespace Echoes.Enemies
{
    public enum CameraState { Patrolling, Alert, Detected }

    public class SecurityCamera : MonoBehaviour
    {
        [Header("Rotation")]
        [SerializeField] private float patrolAngleMin = -45f;
        [SerializeField] private float patrolAngleMax = 45f;
        [SerializeField] private float rotationSpeed = 30f;

        [Header("Detection")]
        [SerializeField] private float coneAngle = 60f;
        [SerializeField] private float coneRange = 5f;
        [SerializeField] private LayerMask playerLayer;
        [SerializeField] private LayerMask obstacleLayer;
        [SerializeField] private float alertDuration = 0.5f;
        [Tooltip("Con este exponente, la duración de alerta ya no es fija: depende de la distancia a la que estés de la cámara. Cerca del propio origen del cono, la misma anchura angular equivale a un arco muchísimo más corto (a igual velocidad, se cruza en mucho menos tiempo) — con una duración fija, eso permitía colarse pegado a la cámara sin llegar nunca a los 0,5s de alerta. Ahora la duración va de ~0 en el origen hasta Alert Duration en el borde (Cone Range), con esta curva (2 = cuadrática): se mantiene baja casi todo el rango y solo sube fuerte cerca del final.")]
        [SerializeField] private float alertDistanceCurveExponent = 2f;

        public CameraState State { get; private set; } = CameraState.Patrolling;
        public float ConeAngle => coneAngle;
        public float ConeRange => coneRange;
        public LayerMask ObstacleLayer => obstacleLayer;
        public float PatrolAngleMin => patrolAngleMin;
        public float PatrolAngleMax => patrolAngleMax;
        public float CurrentSweepAngle => _currentAngle;

        private float _baseAngle;
        private float _currentAngle;
        private float _rotationDirection = 1f;
        private float _alertProgress; // 0-1, en vez de una cuenta atrás fija — ver UpdateAlert
        private float _lastPlayerDistance;
        private VisionCone _visionCone;

        private void Awake()
        {
            _visionCone = GetComponent<VisionCone>();
            _baseAngle = transform.eulerAngles.z;
            _currentAngle = patrolAngleMin;
        }

        private void Update()
        {
            Rotate();
            CheckDetection();
            UpdateAlert();
        }

        private void Rotate()
        {
            _currentAngle += _rotationDirection * rotationSpeed * Time.deltaTime;

            if (_currentAngle >= patrolAngleMax)
            {
                _currentAngle = patrolAngleMax;
                _rotationDirection = -1f;
            }
            else if (_currentAngle <= patrolAngleMin)
            {
                _currentAngle = patrolAngleMin;
                _rotationDirection = 1f;
            }

            transform.rotation = Quaternion.Euler(0f, 0f, _baseAngle + _currentAngle);
        }

        private void CheckDetection()
        {
            if (State == CameraState.Detected) return;

            bool playerInCone = TryGetPlayerInCone(out float distance);

            if (playerInCone)
            {
                _lastPlayerDistance = distance;
                if (State == CameraState.Patrolling)
                {
                    SetState(CameraState.Alert);
                    _alertProgress = 0f;
                }
            }
            else
            {
                if (State == CameraState.Alert)
                    SetState(CameraState.Patrolling);
            }
        }

        // Acumula progreso (0-1) en vez de restar de una cuenta atrás fija: así la "velocidad"
        // de detección puede depender de la distancia del frame actual (RequiredAlertDuration),
        // que se recalcula constantemente si el jugador se mueve dentro del cono.
        private void UpdateAlert()
        {
            if (State != CameraState.Alert) return;

            float required = RequiredAlertDuration(_lastPlayerDistance);
            _alertProgress += required > 0.0001f ? Time.deltaTime / required : 1f;
            if (_alertProgress >= 1f)
                SetState(CameraState.Detected);
        }

        private float RequiredAlertDuration(float distance)
        {
            float t = coneRange > 0f ? Mathf.Clamp01(distance / coneRange) : 1f;
            return alertDuration * Mathf.Pow(t, alertDistanceCurveExponent);
        }

        private bool TryGetPlayerInCone(out float distance)
        {
            distance = 0f;
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, coneRange, playerLayer);

            foreach (var hit in hits)
            {
                if (hit.CompareTag("Echo")) continue;

                Vector2 dirToTarget = (hit.transform.position - transform.position).normalized;
                float angle = Vector2.Angle(transform.up, dirToTarget);

                if (angle > coneAngle * 0.5f) continue;

                float d = Vector2.Distance(transform.position, hit.transform.position);
                RaycastHit2D ray = Physics2D.Raycast(transform.position, dirToTarget, d, obstacleLayer);

                if (ray.collider == null)
                {
                    distance = d;
                    return true;
                }
            }

            return false;
        }

        private void SetState(CameraState newState)
        {
            State = newState;
            _visionCone?.UpdateColor(newState);

            if (newState == CameraState.Detected)
            {
                Debug.Log("[Camera] Jugador detectado. Reiniciando sala.");
                LevelManager.Instance?.TriggerDetection();
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, coneRange);

            Vector3 origin = transform.position;
            Vector3 forward = Application.isPlaying ? transform.up : (Vector3)(Quaternion.Euler(0f, 0f, transform.eulerAngles.z) * Vector2.up);

            Vector3 leftEdge = Quaternion.Euler(0f, 0f, coneAngle * 0.5f) * forward * coneRange;
            Vector3 rightEdge = Quaternion.Euler(0f, 0f, -coneAngle * 0.5f) * forward * coneRange;

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(origin, origin + leftEdge);
            Gizmos.DrawLine(origin, origin + rightEdge);

            const int arcSegments = 12;
            Vector3 prevPoint = origin + leftEdge;
            for (int i = 1; i <= arcSegments; i++)
            {
                float t = coneAngle * (0.5f - (float)i / arcSegments);
                Vector3 point = origin + Quaternion.Euler(0f, 0f, t) * forward * coneRange;
                Gizmos.DrawLine(prevPoint, point);
                prevPoint = point;
            }

            if (!Application.isPlaying)
            {
                Gizmos.color = Color.cyan;
                Vector3 patrolLeft = Quaternion.Euler(0f, 0f, patrolAngleMax) * forward * (coneRange * 0.5f);
                Vector3 patrolRight = Quaternion.Euler(0f, 0f, patrolAngleMin) * forward * (coneRange * 0.5f);
                Gizmos.DrawLine(origin, origin + patrolLeft);
                Gizmos.DrawLine(origin, origin + patrolRight);
            }
        }
    }
}
