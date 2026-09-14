using UnityEngine;

namespace Echoes.Interactables
{
    // Puerta que empieza abierta y se cierra sola, una única vez, en cuanto el jugador termina de
    // cruzarla — para sellar el camino de vuelta a un pasillo anterior una vez la cámara ya está
    // bloqueada a los límites de la sala siguiente (p. ej. el pasillo antes de la Sala 04, donde
    // se podía volver atrás con la cámara ya encajada en la sala nueva). A diferencia de
    // RoomBoundary, no es bidireccional: es un cierre de un solo sentido y para siempre, igual
    // que hace IntroCutscene con la puerta de entrada, pero disparado por el propio jugador en
    // vez de por una cinemática guionizada.
    [RequireComponent(typeof(Collider2D))]
    public class AutoCloseDoor : MonoBehaviour
    {
        [SerializeField] private Door door;
        [Tooltip("Segundos desde que el jugador termina de cruzar hasta que se cierra — un pequeño margen para que no se cierre literalmente pegada a sus talones.")]
        [SerializeField] private float closeDelay = 0.3f;

        [Header("Si se reaparece ya al otro lado (Continue / detección)")]
        [Tooltip("Sin esto, reaparecer ya dentro de la sala (Continue, detección) sin haber cruzado esta puerta en vivo la deja abierta para siempre — el trigger de abajo solo se dispara al cruzarla de verdad. Déjalo vacío para desactivar este chequeo.")]
        [SerializeField] private Transform player;
        [Tooltip("Qué lado del eje Y cuenta como \"ya al otro lado\" — ajusta según la orientación real del pasillo/puerta.")]
        [SerializeField] private bool pastMeansGreaterY = true;

        private bool _triggered;
        private bool _startChecked;

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        // No se hace en Awake/Start: para entonces puede que Continue/detección todavía no haya
        // reposicionado al jugador (eso pasa en el Start() de GameEntryPoint, y el orden entre
        // Start() de scripts distintos no está garantizado). El primer Update() sí que corre
        // siempre después de que TODOS los Start() de la escena hayan terminado.
        private void Update()
        {
            if (_startChecked) return;
            _startChecked = true;

            if (!_triggered && player != null && IsPlayerAlreadyPast())
            {
                _triggered = true;
                if (door != null) door.SetInitialState(false);
            }
        }

        private bool IsPlayerAlreadyPast()
        {
            float diff = player.position.y - transform.position.y;
            return pastMeansGreaterY ? diff > 0f : diff < 0f;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (_triggered || !other.CompareTag("PlayerFeet")) return;
            _triggered = true;
            Invoke(nameof(CloseDoor), closeDelay);
        }

        private void CloseDoor()
        {
            if (door != null) door.Close();
        }
    }
}
