using UnityEngine;

namespace Echoes.Enemies
{
    public class SecurityCameraSprite : MonoBehaviour
    {
        [SerializeField] private SecurityCamera securityCamera;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [Tooltip("Sprites ordenados de PatrolAngleMin a PatrolAngleMax.")]
        [SerializeField] private Sprite[] sweepFrames;
        [Tooltip("Desplazamiento fijo en espacio de mundo respecto al centro de la cámara (no rota con ella).")]
        [SerializeField] private Vector3 worldOffset;
        [Tooltip("Rotación fija del sprite en grados (usa solo múltiplos de 90 para los cardinales: 0=Norte, -90=Este, 180=Sur, 90=Oeste). Para diagonales, deja en 0 y usa Flip X/Flip Y con un set de sprites diagonal.")]
        [SerializeField] private float baseRotation;

        private void LateUpdate()
        {
            transform.rotation = Quaternion.Euler(0f, 0f, baseRotation);
            transform.position = securityCamera.transform.position + worldOffset;

            if (sweepFrames == null || sweepFrames.Length == 0) return;

            float t = Mathf.InverseLerp(securityCamera.PatrolAngleMin, securityCamera.PatrolAngleMax, securityCamera.CurrentSweepAngle);
            int index = Mathf.Clamp(Mathf.RoundToInt(t * (sweepFrames.Length - 1)), 0, sweepFrames.Length - 1);
            spriteRenderer.sprite = sweepFrames[index];
        }
    }
}
