using UnityEngine;

namespace Echoes.Enemies
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class VisionCone : MonoBehaviour
    {
        [Header("Visual")]
        [SerializeField] private int rayCount = 30;
        [SerializeField] private Color patrolColor = new Color(1f, 0f, 0f, 0.3f);
        [SerializeField] private Color alertColor = new Color(1f, 0.6f, 0f, 0.5f);
        [SerializeField] private Color detectedColor = new Color(1f, 0f, 0f, 0.8f);

        private SecurityCamera _camera;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private Material _material;

        private void Awake()
        {
            _camera = GetComponent<SecurityCamera>();
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();

            _material = new Material(Shader.Find("Sprites/Default"));
            _material.color = patrolColor;
            _meshRenderer.material = _material;
        }

        private void LateUpdate()
        {
            DrawCone();
        }

        private void DrawCone()
        {
            float angle = _camera.ConeAngle;
            float range = _camera.ConeRange;

            Mesh mesh = new Mesh();
            Vector3[] vertices = new Vector3[rayCount + 2];
            int[] triangles = new int[rayCount * 3];

            vertices[0] = Vector3.zero;

            float angleStep = angle / rayCount;
            float startAngle = -angle * 0.5f;

            for (int i = 0; i <= rayCount; i++)
            {
                float currentAngle = startAngle + angleStep * i;
                float rad = (currentAngle + 90f) * Mathf.Deg2Rad;
                vertices[i + 1] = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * range;
            }

            for (int i = 0; i < rayCount; i++)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            _meshFilter.mesh = mesh;
        }

        public void UpdateColor(CameraState state)
        {
            _material.color = state switch
            {
                CameraState.Alert => alertColor,
                CameraState.Detected => detectedColor,
                _ => patrolColor
            };
        }
    }
}
