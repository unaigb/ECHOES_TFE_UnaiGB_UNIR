using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Echoes.Camera;
using Echoes.Interactables;

namespace Echoes.Managers
{
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        public event Action<string> OnRoomChanged;
        public string CurrentRoomName { get; private set; }

        [Header("Detection")]
        [SerializeField] private DetectionScreen detectionScreen;

        [Header("Room Transitions")]
        [SerializeField] private CameraFollow cameraFollow;
        [SerializeField] private Rigidbody2D playerRigidbody;
        [SerializeField] private RoomBoundary[] initialRoomExits;

        [Header("Debug — probar una sala directamente (dejar vacío para la partida real)")]
        [SerializeField] private Transform debugStartPosition;
        [SerializeField] private RoomBoundary[] debugStartRoomExits;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

#if UNITY_EDITOR
            if (debugStartPosition != null)
            {
                playerRigidbody.position = debugStartPosition.position;
                ApplyExitBarriers(debugStartRoomExits.Length > 0 ? debugStartRoomExits : initialRoomExits);
                return;
            }
#endif
            ApplyExitBarriers(initialRoomExits);
        }

        public void TriggerDetection()
        {
            if (detectionScreen != null)
                detectionScreen.Show(RestartLevel);
            else
                RestartLevel();
        }

        public void RestartLevel()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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

        private bool _zoomOutLocked;

        private void ApplyExitBarriers(RoomBoundary[] exits)
        {
            if (exits == null) return;

            string roomName = null;
            foreach (var exit in exits)
            {
                if (exit == null) continue;
                cameraFollow.ApplyBarrier(exit.Direction, exit.GetBarrierValue());
                if (exit.ZoomOutInThisRoom) _zoomOutLocked = true;
                if (!string.IsNullOrEmpty(exit.RoomName)) roomName = exit.RoomName;
            }
            cameraFollow.SetZoomedOut(_zoomOutLocked);

            if (roomName != null)
            {
                CurrentRoomName = roomName;
                OnRoomChanged?.Invoke(roomName);
            }
        }
    }
}
