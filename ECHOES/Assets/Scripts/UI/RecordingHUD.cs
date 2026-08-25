using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Echoes.Echo;

namespace Echoes.UI
{
    public class RecordingHUD : MonoBehaviour
    {
        [SerializeField] private EchoRecorder recorder;
        [SerializeField] private GameObject barContainer;
        [SerializeField] private Image fillBar;
        [SerializeField] private TextMeshProUGUI stateLabel;

        private static readonly Color RecordingColor = new Color(1f, 0.15f, 0.15f, 1f);
        private static readonly Color RecordedColor = new Color(0.1f, 0.9f, 1f, 1f);
        private static readonly Color PlayingColor = new Color(0.1f, 0.9f, 1f, 0.6f);

        private float _pulseTimer;

        private void LateUpdate()
        {
            UpdateHUD();
        }

        private void UpdateHUD()
        {
            if (recorder == null) return;

            switch (recorder.State)
            {
                case EchoState.Idle:
                    barContainer.SetActive(false);
                    stateLabel.text = "";
                    break;

                case EchoState.Recording:
                    barContainer.SetActive(true);
                    _pulseTimer += Time.deltaTime * 5f;
                    fillBar.fillAmount = recorder.RecordingProgress;
                    fillBar.color = RecordingColor;
                    stateLabel.text = Mathf.Sin(_pulseTimer) > 0f ? "● RECORDING" : "";
                    stateLabel.color = RecordingColor;
                    break;

                case EchoState.Recorded:
                    barContainer.SetActive(true);
                    fillBar.fillAmount = 1f;
                    fillBar.color = RecordedColor;
                    stateLabel.text = "ECHO READY";
                    stateLabel.color = RecordedColor;
                    break;

                case EchoState.Playing:
                    barContainer.SetActive(true);
                    _pulseTimer += Time.deltaTime * 3f;
                    fillBar.fillAmount = 1f;
                    fillBar.color = new Color(PlayingColor.r, PlayingColor.g, PlayingColor.b,
                        Mathf.Lerp(0.3f, 0.8f, (Mathf.Sin(_pulseTimer) + 1f) * 0.5f));
                    stateLabel.text = "ECHO ACTIVE";
                    stateLabel.color = RecordedColor;
                    break;
            }
        }
    }
}
