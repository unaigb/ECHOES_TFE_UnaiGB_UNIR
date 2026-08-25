using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System;

namespace Echoes.Managers
{
    public class DetectionScreen : MonoBehaviour
    {
        [SerializeField] private Image overlay;
        [SerializeField] private TextMeshProUGUI detectionText;

        private static readonly Color RedFlash = new Color(0.8f, 0f, 0f, 0.6f);
        private static readonly Color BlackOverlay = new Color(0f, 0f, 0f, 0.95f);
        private static readonly Color NeonRed = new Color(1f, 0.05f, 0.05f, 1f);

        private void Awake()
        {
            overlay.color = Color.clear;
            detectionText.alpha = 0f;
            gameObject.SetActive(false);
        }

        public void Show(Action onComplete)
        {
            gameObject.SetActive(true);
            StartCoroutine(DetectionSequence(onComplete));
        }

        private IEnumerator DetectionSequence(Action onComplete)
        {
            // Flash rojo
            overlay.color = RedFlash;
            yield return new WaitForSeconds(0.15f);
            overlay.color = Color.clear;
            yield return new WaitForSeconds(0.08f);
            overlay.color = RedFlash;
            yield return new WaitForSeconds(0.15f);

            // Fade a negro
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 3f;
                overlay.color = Color.Lerp(RedFlash, BlackOverlay, t);
                yield return null;
            }
            overlay.color = BlackOverlay;

            // Texto aparece
            detectionText.text = "DETECTED";
            detectionText.color = NeonRed;

            t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 4f;
                detectionText.alpha = Mathf.Lerp(0f, 1f, t);
                yield return null;
            }

            yield return new WaitForSeconds(1.8f);

            // Fade out y reinicio
            t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 2f;
                overlay.color = Color.Lerp(BlackOverlay, Color.black, t);
                yield return null;
            }

            onComplete?.Invoke();
        }
    }
}
