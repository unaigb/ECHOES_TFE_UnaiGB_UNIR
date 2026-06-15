using UnityEngine;

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

        private void Awake()
        {
            _cam = GetComponent<UnityEngine.Camera>();
            _targetSize = defaultSize;
            _cam.orthographicSize = defaultSize;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 desired = new Vector3(target.position.x, target.position.y, transform.position.z);
            transform.position = Vector3.Lerp(transform.position, desired, damping * Time.deltaTime);

            _cam.orthographicSize = Mathf.Lerp(_cam.orthographicSize, _targetSize, zoomSpeed * Time.deltaTime);
        }

        public void SetZoomedOut(bool zoomedOut)
        {
            _targetSize = zoomedOut ? zoomedOutSize : defaultSize;
        }
    }
}
