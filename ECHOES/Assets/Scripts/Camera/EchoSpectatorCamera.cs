using UnityEngine;
using UnityEngine.InputSystem;
using Echoes.Echo;
using Echoes.Player;

namespace Echoes.Camera
{
    public class EchoSpectatorCamera : MonoBehaviour
    {
        [SerializeField] private CameraFollow cameraFollow;
        [SerializeField] private EchoRecorder echoRecorder;
        [Tooltip("Para no entrar en modo espectador mientras el input del jugador está bloqueado (diálogos, cinemáticas).")]
        [SerializeField] private PlayerInputHandler playerInput;

        private InputAction _spectateAction;

        private void Awake()
        {
            _spectateAction = InputSystem.actions.FindAction("Player/SpectateEcho");
        }

        private void OnEnable()
        {
            _spectateAction.started += OnSpectateStart;
            _spectateAction.canceled += OnSpectateEnd;
        }

        private void OnDisable()
        {
            _spectateAction.started -= OnSpectateStart;
            _spectateAction.canceled -= OnSpectateEnd;
        }

        private void OnSpectateStart(InputAction.CallbackContext ctx)
        {
            if (playerInput != null && !playerInput.InputEnabled) return;
            if (echoRecorder.State != EchoState.Playing) return;
            cameraFollow.SetSpectateTarget(echoRecorder.ActiveEchoTransform);
        }

        private void OnSpectateEnd(InputAction.CallbackContext ctx)
        {
            cameraFollow.SetSpectateTarget(null);
        }
    }
}
