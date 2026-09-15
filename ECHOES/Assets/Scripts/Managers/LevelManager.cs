using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Echoes.Camera;
using Echoes.Interactables;
using Echoes.Saving;
using Echoes.Player;

namespace Echoes.Managers
{
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        public event Action<string> OnRoomChanged;
        public string CurrentRoomName { get; private set; }
        // Última tanda de RoomBoundary aplicada — para que EchoRecorder pueda guardar "en qué
        // sala estaba la cámara" al empezar a grabar y restaurarlo tal cual al terminar, sin
        // depender de un cruce físico por un trigger (ver ReapplyExits más abajo).
        public RoomBoundary[] CurrentExits { get; private set; }
        // Cierto cuando Awake ha usado el salto de sala de depuración (debugStartPosition).
        // GameEntryPoint lo usa para no reproducir la cinemática de entrada por encima.
        public bool IsDebugJump { get; private set; }

        [Header("Detection")]
        [SerializeField] private DetectionScreen detectionScreen;
        [Tooltip("Segundos tras cargar la sala en los que se ignora una detección. Sin esto, si el punto de reaparición cae dentro del cono de una cámara (p. ej. Sala 03), esta te detecta de nuevo a los pocos frames de recargar, dispara otro reinicio, y así sin parar — un bucle de recargas de escena que no lanza ninguna excepción, así que no se ve nada en la consola, pero bloquea el Editor por completo.")]
        [SerializeField] private float detectionGraceTime = 1.5f;
        private float _detectionGraceUntil;
        // Si dos conos de cámara detectan al jugador el mismo frame (o casi), sin esto ambos
        // llaman a TriggerDetection() — dos DetectionSequence corriendo a la vez sobre el mismo
        // DetectionScreen (doble flash, doble sonido) y dos RestartLevel() seguidos. Se resetea
        // solo al recargar la escena, como el resto del estado de LevelManager.
        private bool _detectionTriggered;
        private PlayerInputHandler _playerInput;

        [Header("Room Transitions")]
        [SerializeField] private CameraFollow cameraFollow;
        [SerializeField] private Rigidbody2D playerRigidbody;
        [SerializeField] private RoomBoundary[] initialRoomExits;

        [Header("Debug — probar una sala directamente (dejar vacío para la partida real)")]
        [SerializeField] private Transform debugStartPosition;
        [SerializeField] private RoomBoundary[] debugStartRoomExits;

        [Header("Guardado — punto de reaparición de cada sala")]
        [Tooltip("Uno por sala. RoomName debe coincidir exactamente con el RoomBoundary.RoomName de esa sala.")]
        [SerializeField] private RoomSaveInfo[] roomSaveInfos;

        [Serializable]
        public class RoomSaveInfo
        {
            public string roomName;
            public Transform spawnPoint;
            public RoomBoundary[] exits;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _detectionGraceUntil = Time.time + detectionGraceTime;
            _playerInput = playerRigidbody != null ? playerRigidbody.GetComponent<PlayerInputHandler>() : null;
            // Se retira en GameEntryPoint.Reveal() — ver comentario en GameFlow.SilentSetup.
            GameFlow.SilentSetup = true;

#if UNITY_EDITOR
            // Sin el "&& !GameFlow.ContinueRequested", una recarga real de Continue/detección
            // vuelve a caer aquí mientras debugStartPosition siga puesto en el Inspector (algo
            // habitual mientras se prueba una sala concreta) — GameEntryPoint la trataba entonces
            // como si fuera el propio salto de depuración y se saltaba el fundido a negro.
            if (debugStartPosition != null && !GameFlow.ContinueRequested)
            {
                IsDebugJump = true;
                playerRigidbody.position = debugStartPosition.position;
                ApplyExitBarriers(debugStartRoomExits.Length > 0 ? debugStartRoomExits : initialRoomExits);
                return;
            }
#endif
            ApplyExitBarriers(initialRoomExits);
        }

        public void TriggerDetection()
        {
            if (Time.time < _detectionGraceUntil) return;
            if (_detectionTriggered) return;
            _detectionTriggered = true;

            // El jugador queda "congelado" en cuanto se dispara la detección — antes se podía
            // seguir moviendo durante todo el flash/fundido de DetectionScreen.
            if (_playerInput != null) _playerInput.InputEnabled = false;

            if (detectionScreen != null)
                detectionScreen.Show(RestartLevel);
            else
                RestartLevel();
        }

        // Al ser detectado: recarga la escena (resetea todos los puzles a su estado inicial)
        // pero reaprovecha el sistema de guardado para reaparecer en la última sala alcanzada
        // en vez de siempre en la Sala 01 — misma bandera que usa "Continue" desde el título.
        public void RestartLevel()
        {
            GameFlow.LeavingScene = true;
            GameFlow.ContinueRequested = true;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        // Llamado por SaveManager al pulsar "Continue": reposiciona al jugador en la sala
        // guardada y reaplica sus bounds, sin transición — equivalente a reiniciar esa sala.
        public bool TryLoadRoom(string roomName)
        {
            if (roomSaveInfos == null) return false;
            foreach (var info in roomSaveInfos)
            {
                if (info == null || info.roomName != roomName) continue;
                if (info.spawnPoint != null) playerRigidbody.position = info.spawnPoint.position;
                ApplyExitBarriers(info.exits);
                return true;
            }
            Debug.LogWarning($"[LevelManager] No hay RoomSaveInfo para \"{roomName}\" — revisa el array en el Inspector.");
            return false;
        }

        // Sin barrido guionizado: los bounds de sala son contiguos por construcción (el muro de
        // cruce es a la vez el límite "saliente" de la sala vieja y el "entrante" de la nueva),
        // así que basta con actualizarlos al vuelo — la cámara sigue al jugador exactamente
        // igual que el resto del tiempo, sin ninguna animación ni freeze de input especial.
        public void StartRoomTransition(RoomBoundary crossedExit, RoomBoundary[] nextRoomExits, ExitDirection panDirection)
        {
            RegisterRoomBarriers(crossedExit, nextRoomExits, panDirection);
        }

        private void RegisterRoomBarriers(RoomBoundary crossedExit, RoomBoundary[] nextRoomExits, ExitDirection panDirection)
        {
            cameraFollow.ClearLimits();

            // El muro de vuelta de la sala nueva queda justo donde cruzaste, en el lado opuesto al barrido.
            cameraFollow.ApplyBarrier(RoomBoundary.Flip(panDirection), crossedExit.GetBarrierValue());

            ApplyExitBarriers(nextRoomExits);
        }

        // Vuelve a aplicar una tanda de salidas ya conocida, sin pasar por ningún cruce físico.
        // Caso de uso: el jugador cruza "hacia atrás" a la sala anterior mientras graba un eco
        // (la puerta se lo permite a propósito, ver RoomBoundary.previousRoomExits), y al parar
        // la grabación EchoRecorder lo teletransporta de vuelta a donde empezó a grabar — ese
        // teletransporte no dispara ningún trigger, así que sin esto la cámara se quedaba con
        // los límites de la sala "de atrás" aunque el jugador ya estuviera de vuelta.
        public void ReapplyExits(RoomBoundary[] exits) => ApplyExitBarriers(exits);

        private void ApplyExitBarriers(RoomBoundary[] exits)
        {
            if (exits == null) return;
            CurrentExits = exits;

            // Recalculado desde cero en cada llamada, no acumulado: si no, una sala con zoom-out
            // (p.ej. Sala 03) deja la cámara alejada para siempre en todas las salas siguientes,
            // y ese "arrastre" además desaparece solo en una recarga de escena (detección,
            // Continue), dando un tamaño distinto según cómo se haya llegado a la sala.
            bool zoomOut = false;
            string roomName = null;
            foreach (var exit in exits)
            {
                if (exit == null) continue;
                cameraFollow.ApplyBarrier(exit.Direction, exit.GetBarrierValue());
                if (exit.ZoomOutInThisRoom) zoomOut = true;
                if (!string.IsNullOrEmpty(exit.RoomName)) roomName = exit.RoomName;
            }
            cameraFollow.SetZoomedOut(zoomOut);

            // Solo avisa de verdad si la sala cambia. ReapplyExits() (fix del eco cruzando de
            // vuelta a una sala anterior) puede reaplicar la MISMA sala en la que ya estábamos —
            // sin este chequeo, el evento se disparaba igualmente para la sala de la que ya
            // veníamos, cosa redundante en el mejor caso.
            if (roomName != null && roomName != CurrentRoomName)
            {
                CurrentRoomName = roomName;
                OnRoomChanged?.Invoke(roomName);
            }
        }
    }
}
