using UnityEngine;

namespace Echoes.Interactables
{
    // Ordena el sprite dentro de su Sorting Layer según su posición Y: cuanto más abajo en
    // pantalla (más al sur), más alto el Order in Layer, así que se dibuja delante. Soluciona
    // que el jugador y una caja empujable no se tapen bien según desde qué lado se empuje.
    //
    // Pon uno en el jugador (en el SpriteRenderer del sprite, normalmente en SpriteVisual) y
    // otro en cada caja movible — todos deben estar en la MISMA Sorting Layer para que el
    // Order in Layer los compare entre sí.
    [RequireComponent(typeof(SpriteRenderer))]
    public class YSorter : MonoBehaviour
    {
        [Tooltip("Cuanto más alto, más sensible el orden a pequeños cambios de Y. 100 va bien para el tamaño de escena actual.")]
        [SerializeField] private float precision = 100f;
        [Tooltip("Opcional: punto de referencia para el cálculo (p.ej. los pies), si no se deja vacío usa este mismo transform.")]
        [SerializeField] private Transform sortPoint;

        private SpriteRenderer _renderer;

        private void Awake() => _renderer = GetComponent<SpriteRenderer>();

        private void LateUpdate()
        {
            Transform t = sortPoint != null ? sortPoint : transform;
            _renderer.sortingOrder = -(int)(t.position.y * precision);
        }
    }
}
