using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Echoes.Camera;
using Echoes.Player;
using Echoes.Interactables;

namespace Echoes.Managers
{
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        [Header("Detection")]
        [SerializeField] private DetectionScreen detectionScreen;

        [Header("Room Transitions")]
        [SerializeField] private CameraFollow cameraFollow;
        [SerializeField] private PlayerInputHandler playerInput;
        [SerializeField] private Rigidbody2D playerRigidbody;
        [SerializeField] private ExitZone[] initialRoomExits;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

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

        public void StartRoomTransition(ExitZone crossedExit, ExitZone[] nextRoomExits)
        {
            StartCoroutine(RoomTransitionSequence(crossedExit, nextRoomExits));
        }

        private IEnumerator RoomTransitionSequence(ExitZone crossedExit, ExitZone[] nextRoomExits)
        {
            float duration = 0.35f;

            playerInput.InputEnabled = false;
            playerRigidbody.linearVelocity = Vector2.zero;

            yield return StartCoroutine(cameraFollow.TransitionToRoom(crossedExit.Direction, duration));

            RegisterRoomBarriers(crossedExit, nextRoomExits);
            playerInput.InputEnabled = true;
        }

        private void RegisterRoomBarriers(ExitZone crossedExit, ExitZone[] nextRoomExits)
        {
            cameraFollow.ClearLimits();

            ExitDirection flipped = crossedExit.Direction switch
            {
                ExitDirection.Right => ExitDirection.Left,
                ExitDirection.Left  => ExitDirection.Right,
                ExitDirection.Up    => ExitDirection.Down,
                ExitDirection.Down  => ExitDirection.Up,
                _ => crossedExit.Direction
            };
            cameraFollow.ApplyBarrier(flipped, crossedExit.GetBarrierValue());

            ApplyExitBarriers(nextRoomExits);
        }

        private void ApplyExitBarriers(ExitZone[] exits)
        {
            if (exits == null) return;
            foreach (var exit in exits)
                if (exit != null)
                    cameraFollow.ApplyBarrier(exit.Direction, exit.GetBarrierValue());
        }
    }
}
