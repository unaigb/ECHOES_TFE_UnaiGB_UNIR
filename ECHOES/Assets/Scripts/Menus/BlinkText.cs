using UnityEngine;
using UnityEngine.UI;

namespace Echoes.Menus
{
    // Parpadeo del "Press Start" al estilo Pokémon NDS. A pesar del nombre, vale tanto para
    // texto como para una imagen: Image y TextMeshProUGUI heredan las dos de Graphic, así que
    // este script sirve para cualquiera de las dos sin cambiar nada.
    // Sin dependencias de tiempo de juego (usa Time.unscaledTime).
    [RequireComponent(typeof(Graphic))]
    public class BlinkText : MonoBehaviour
    {
        [SerializeField] private float blinkSpeed = 2f;
        [Tooltip("Alpha mínimo del parpadeo — 0 desaparece del todo, más alto solo atenúa.")]
        [Range(0f, 1f)] [SerializeField] private float minAlpha = 0f;

        private Graphic _graphic;

        private void Awake() => _graphic = GetComponent<Graphic>();

        private void Update()
        {
            float t = (Mathf.Sin(Time.unscaledTime * blinkSpeed) + 1f) * 0.5f;
            float alpha = Mathf.Lerp(minAlpha, 1f, t);
            Color c = _graphic.color;
            c.a = alpha;
            _graphic.color = c;
        }
    }
}
