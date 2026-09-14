using UnityEngine;
using UnityEngine.UI;

namespace Echoes.UI
{
    // Para paneles de cómic "en GIF": Unity no importa archivos .gif directamente, así que hay
    // que exportar los fotogramas del GIF como imágenes sueltas (cualquier conversor GIF→PNG
    // online vale, p. ej. ezgif.com/gif-to-png) e importarlas como Sprites — este componente los
    // reproduce en bucle sobre el Image del panel. Independiente del fundido de EndingSequence:
    // eso solo controla el alpha del CanvasGroup del panel, esto solo cambia qué sprite se ve, así
    // que un panel puede ser estático o animado sin que EndingSequence tenga que saber nada.
    [RequireComponent(typeof(Image))]
    public class AnimatedPanel : MonoBehaviour
    {
        [Tooltip("Los fotogramas del GIF, en orden.")]
        [SerializeField] private Sprite[] frames;
        [SerializeField] private float framesPerSecond = 10f;
        [Tooltip("Desmarca para que se quede congelado en el último fotograma al llegar al final, en vez de repetirse.")]
        [SerializeField] private bool loop = true;

        private Image _image;
        private int _index;
        private float _timer;

        private void Awake()
        {
            _image = GetComponent<Image>();
            if (frames != null && frames.Length > 0) _image.sprite = frames[0];
        }

        private void Update()
        {
            if (frames == null || frames.Length < 2 || framesPerSecond <= 0f) return;

            _timer += Time.deltaTime;
            float frameTime = 1f / framesPerSecond;
            if (_timer < frameTime) return;
            _timer -= frameTime;

            _index++;
            if (_index >= frames.Length)
            {
                if (!loop) { _index = frames.Length - 1; return; }
                _index = 0;
            }
            _image.sprite = frames[_index];
        }
    }
}
