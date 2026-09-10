using UnityEngine;
using UnityEngine.InputSystem;
using Echoes.Echo;

namespace Echoes.Camera
{
    public class EchoSpectatorCamera : MonoBehaviour
    {
        [SerializeField] private CameraFollow cameraFollow;
        [SerializeField] private EchoRecorder echoRecorder;

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
            if (echoRecorder.State != EchoState.Playing) return;
            cameraFollow.SetSpectateTarget(echoRecorder.ActiveEchoTransform);
        }

        private void OnSpectateEnd(InputAction.CallbackContext ctx)
        {
            cameraFollow.SetSpectateTarget(null);
        }
    }
}
