using UnityEngine;
using System.Collections;

namespace Echoes.UI
{
    public class LetterboxBars : MonoBehaviour
    {
        [SerializeField] private RectTransform topBar;
        [SerializeField] private RectTransform bottomBar;
        [SerializeField] private float barHeight = 80f;
        [SerializeField] private float slideTime = 0.5f;

        private void Awake()
        {
            SetHeights(barHeight);
        }

        public IEnumerator Show()
        {
            yield return Slide(0f, barHeight);
        }

        public IEnumerator Hide()
        {
            yield return Slide(barHeight, 0f);
        }

        private IEnumerator Slide(float from, float to)
        {
            float t = 0f;
            while (t < slideTime)
            {
                t += Time.deltaTime;
                SetHeights(Mathf.Lerp(from, to, t / slideTime));
                yield return null;
            }
            SetHeights(to);
        }

        private void SetHeights(float height)
        {
            topBar.sizeDelta = new Vector2(topBar.sizeDelta.x, height);
            bottomBar.sizeDelta = new Vector2(bottomBar.sizeDelta.x, height);
        }
    }
}
