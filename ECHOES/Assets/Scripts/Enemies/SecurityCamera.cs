using UnityEngine;

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
        [SerializeField] private float alertDuration = 1.5f;

        public CameraState State { get; private set; } = CameraState.Patrolling;
        public float ConeAngle => coneAngle;
        public float ConeRange => coneRange;

        private float _currentAngle;
        private float _rotationDirection = 1f;
        private float _alertTimer;
        private VisionCone _visionCone;

        private void Awake()
        {
            _visionCone = GetComponent<VisionCone>();
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

            transform.rotation = Quaternion.Euler(0f, 0f, _currentAngle);
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
                LevelManager.Instance?.RestartLevel();
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, coneRange);
        }
    }
}
