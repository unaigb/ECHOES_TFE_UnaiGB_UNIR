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
        private float _alertTimer;
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

            bool playerInCone = IsPlayerInCone();

            if (playerInCone)
            {
                if (State == CameraState.Patrolling)
                {
                    SetState(CameraState.Alert);
                    _alertTimer = alertDuration;
                }
            }
            else
            {
                if (State == CameraState.Alert)
                    SetState(CameraState.Patrolling);
            }
        }

        private void UpdateAlert()
        {
            if (State != CameraState.Alert) return;

            _alertTimer -= Time.deltaTime;
            if (_alertTimer <= 0f)
                SetState(CameraState.Detected);
        }

        private bool IsPlayerInCone()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, coneRange, playerLayer);

            foreach (var hit in hits)
            {
                if (hit.CompareTag("Echo")) continue;

                Vector2 dirToTarget = (hit.transform.position - transform.position).normalized;
                float angle = Vector2.Angle(transform.up, dirToTarget);

                if (angle > coneAngle * 0.5f) continue;

                float distance = Vector2.Distance(transform.position, hit.transform.position);
                RaycastHit2D ray = Physics2D.Raycast(transform.position, dirToTarget, distance, obstacleLayer);

                if (ray.collider == null)
                    return true;
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
