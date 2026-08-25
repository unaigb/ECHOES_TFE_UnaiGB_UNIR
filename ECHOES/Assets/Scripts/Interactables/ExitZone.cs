using UnityEngine;
using Echoes.Managers;

namespace Echoes.Interactables
{
    public enum ExitDirection { Right, Left, Up, Down }

    [RequireComponent(typeof(Collider2D))]
    public class ExitZone : MonoBehaviour
    {
        [SerializeField] private ExitDirection direction;
        [SerializeField] private ExitZone[] nextRoomExits;

        private bool _used;

        public ExitDirection Direction => direction;

        public float GetBarrierValue() => direction switch
        {
            ExitDirection.Right or ExitDirection.Left => transform.position.x,
            ExitDirection.Up   or ExitDirection.Down  => transform.position.y,
            _ => 0f
        };

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_used || !other.CompareTag("Player")) return;
            _used = true;
            LevelManager.Instance?.StartRoomTransition(this, nextRoomExits);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // Trigger zone (amarillo)
            BoxCollider2D col = GetComponent<BoxCollider2D>();
            Vector3 size = col != null ? (Vector3)col.size : new Vector3(1f, 1f, 0f);
            Gizmos.color = new Color(1f, 0.9f, 0f, 0.35f);
            Gizmos.DrawCube(transform.position, size);
            Gizmos.color = new Color(1f, 0.9f, 0f, 1f);
            Gizmos.DrawWireCube(transform.position, size);

            // Flecha de dirección
            Gizmos.color = Color.yellow;
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
