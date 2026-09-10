using UnityEngine;
using Echoes.Managers;

namespace Echoes.Interactables
{
    public enum ExitDirection { Right, Left, Up, Down }

    [RequireComponent(typeof(Collider2D))]
    public class RoomBoundary : MonoBehaviour
    {
        [SerializeField] private ExitDirection direction;
        [SerializeField] private RoomBoundary[] nextRoomExits;
        [Tooltip("Opcional. El set de RoomBoundary de la sala de la que se viene (los mismos objetos que ya uses como Initial Room Exits o como Next Room Exits de la puerta anterior). Si lo rellenas, cruzar esta puerta hacia atrás también funciona correctamente (cámara incluida) — útil para puzles que exigan volver. Si lo dejas vacío, cruzar hacia atrás no hace nada (puerta de un solo sentido).")]
        [SerializeField] private RoomBoundary[] previousRoomExits;
        [Tooltip("Si esta salida pertenece a una sala que necesita zoom-out de cámara (ej. Sala 03), márcalo en TODAS las RoomBoundary de esa sala.")]
        [SerializeField] private bool zoomOutInThisRoom;
        [Tooltip("Nombre de la sala a la que pertenece esta salida (ej. \"Sala 03\"), para el indicador de sala actual. Repite el mismo texto en TODAS las RoomBoundary de esa sala.")]
        [SerializeField] private string roomName;
        [Tooltip("Desmarca esto solo para las salidas funcionales (con puerta a otra sala). Márcalo para los lados de la sala que son solo pared: sirve únicamente para fijar el límite de cámara en esa dirección, no dispara ninguna transición.")]
        [SerializeField] private bool wallOnly;

        private int _entrySide;

        public ExitDirection Direction => direction;
        public bool ZoomOutInThisRoom => zoomOutInThisRoom;
        public string RoomName => roomName;

        public float GetBarrierValue() => direction switch
        {
            ExitDirection.Right or ExitDirection.Left => transform.position.x,
            ExitDirection.Up   or ExitDirection.Down  => transform.position.y,
            _ => 0f
        };

        public static ExitDirection Flip(ExitDirection d) => d switch
        {
            ExitDirection.Right => ExitDirection.Left,
            ExitDirection.Left  => ExitDirection.Right,
            ExitDirection.Up    => ExitDirection.Down,
            ExitDirection.Down  => ExitDirection.Up,
            _ => d
        };

        private int SideOf(Vector2 pos)
        {
            float signed = direction is ExitDirection.Right or ExitDirection.Left
                ? pos.x - transform.position.x
                : pos.y - transform.position.y;
            return signed >= 0f ? 1 : -1;
        }

        // No decide nada al entrar, solo registra por qué lado ha entrado. Entrar a medias y
        // volver por el mismo lado no debe disparar ninguna transición.
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (wallOnly || !other.CompareTag("PlayerFeet")) return;
            _entrySide = SideOf(other.transform.position);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (wallOnly || !other.CompareTag("PlayerFeet")) return;

            int exitSide = SideOf(other.transform.position);
            if (exitSide == _entrySide) return; // salió por el mismo lado por el que entró: no hubo cruce real

            bool forwardSideIsPositive = direction is ExitDirection.Right or ExitDirection.Up;
            bool crossedForward = forwardSideIsPositive ? exitSide > 0 : exitSide < 0;

            if (crossedForward)
            {
                LevelManager.Instance?.StartRoomTransition(this, nextRoomExits, direction);
            }
            else if (previousRoomExits != null && previousRoomExits.Length > 0)
            {
                LevelManager.Instance?.StartRoomTransition(this, previousRoomExits, Flip(direction));
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // Trigger zone: amarillo = puerta funcional, azul = solo límite de cámara (pared)
            Color baseColor = wallOnly ? new Color(0.2f, 0.6f, 1f) : new Color(1f, 0.9f, 0f);

            BoxCollider2D col = GetComponent<BoxCollider2D>();
            Vector3 size = col != null ? (Vector3)col.size : new Vector3(1f, 1f, 0f);
            Gizmos.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.35f);
            Gizmos.DrawCube(transform.position, size);
            Gizmos.color = baseColor;
            Gizmos.DrawWireCube(transform.position, size);

            // Flecha de dirección
            Gizmos.color = baseColor;
            Vector3 arrow = direction switch
            {
                ExitDirection.Right => Vector3.right,
                ExitDirection.Left  => Vector3.left,
                ExitDirection.Up    => Vector3.up,
                ExitDirection.Down  => Vector3.down,
                _ => Vector3.zero
            };
            Gizmos.DrawRay(transform.position, arrow * 0.8f);

        }
#endif
    }
}
