using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Echoes.Echo;

namespace Echoes.UI
{
    public class RecordingHUD : MonoBehaviour
    {
        [SerializeField] private EchoRecorder recorder;
        [SerializeField] private Image fillBar;
        [SerializeField] private TextMeshProUGUI stateLabel;
        [SerializeField] private Transform playerTransform;

        private static readonly Color RecordingColor = new Color(1f, 0.15f, 0.15f, 1f);
        private static readonly Color RecordedColor = new Color(0.1f, 0.9f, 1f, 1f);
        private static readonly Color PlayingColor = new Color(0.1f, 0.9f, 1f, 0.6f);

        private float _pulseTimer;
        private Camera _cam;

        private void Awake()
        {
            _cam = Camera.main;
        }

        private void LateUpdate()
        {
            FollowPlayer();
            UpdateHUD();
        }

        private void FollowPlayer()
        {
            if (playerTransform == null || _cam == null) return;
            transform.position = playerTransform.position + Vector3.up * 0.7f;
        }

        private void UpdateHUD()
        {
            if (recorder == null) return;

            switch (recorder.State)
            {
                case EchoState.Idle:
                    fillBar.fillAmount = 0f;
                    stateLabel.text = "";
                    break;

                case EchoState.Recording:
                    _pulseTimer += Time.deltaTime * 5f;
                    fillBar.fillAmount = recorder.RecordingProgress;
                    fillBar.color = RecordingColor;
                    stateLabel.text = Mathf.Sin(_pulseTimer) > 0f ? "● REC" : "";
                    stateLabel.color = RecordingColor;
                    break;

                case EchoState.Recorded:
                    fillBar.fillAmount = recorder.RecordingProgress;
                    fillBar.color = RecordedColor;
                    stateLabel.text = "◆ ECO LISTO";
                    stateLabel.color = RecordedColor;
                    break;

                case EchoState.Playing:
                    _pulseTimer += Time.deltaTime * 3f;
                    fillBar.fillAmount = recorder.RecordingProgress;
                    fillBar.color = new Color(PlayingColor.r, PlayingColor.g, PlayingColor.b,
                        Mathf.Lerp(0.3f, 0.8f, (Mathf.Sin(_pulseTimer) + 1f) * 0.5f));
                    stateLabel.text = "► ECO ACTIVO";
                    stateLabel.color = RecordedColor;
                    break;
            }
        }
    }
}
