using UnityEngine;
using UnityEngine.InputSystem;

namespace Echoes.Echo
{
    public class EchoRecorder : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float maxRecordingDuration = 15f;
        [SerializeField] private GameObject echoPrefab;

        public EchoState State { get; private set; } = EchoState.Idle;
        public float RecordingProgress => maxRecordingDuration > 0 ? _recordingTimer / maxRecordingDuration : 0f;

        private RecordingData _data = new();
        private float _recordingTimer;
        private Rigidbody2D _rb;
        private Player.PlayerInteraction _playerInteraction;
        private InputAction _recordAction;
        private InputAction _deployAction;
        private InputAction _rewindAction;
        private GameObject _activeEcho;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _playerInteraction = GetComponent<Player.PlayerInteraction>();
            _recordAction = InputSystem.actions.FindAction("Player/Record");
            _deployAction = InputSystem.actions.FindAction("Player/DeployEcho");
            _rewindAction = InputSystem.actions.FindAction("Player/Rewind");
        }

        private void OnEnable()
        {
            _recordAction.performed += OnRecord;
            _deployAction.performed += OnDeploy;
            _rewindAction.performed += OnRewind;
            if (_playerInteraction != null)
                _playerInteraction.OnInteracted += OnPlayerInteracted;
        }

        private void OnDisable()
        {
            _recordAction.performed -= OnRecord;
            _deployAction.performed -= OnDeploy;
            _rewindAction.performed -= OnRewind;
            if (_playerInteraction != null)
                _playerInteraction.OnInteracted -= OnPlayerInteracted;
        }

        private void FixedUpdate()
        {
            if (State != EchoState.Recording) return;

            _recordingTimer += Time.fixedDeltaTime;

            _data.frames.Add(new FrameSnapshot
            {
                time = _recordingTimer,
                position = (Vector2)transform.position
            });

            if (_recordingTimer >= maxRecordingDuration)
                StopRecording();
        }

        private void OnRecord(InputAction.CallbackContext ctx)
        {
            if (State == EchoState.Idle || State == EchoState.Recorded)
                StartRecording();
            else if (State == EchoState.Recording)
                StopRecording();
        }

        private void OnDeploy(InputAction.CallbackContext ctx)
        {
            if (State != EchoState.Recorded) return;

            if (_activeEcho != null)
                Destroy(_activeEcho);

            _activeEcho = Instantiate(echoPrefab, _data.startPosition, Quaternion.identity);
            _activeEcho.GetComponent<EchoPlayback>().Play(_data);

            State = EchoState.Playing;
        }

        private void OnRewind(InputAction.CallbackContext ctx)
        {
            if (State == EchoState.Idle) return;

            if (_activeEcho != null)
                Destroy(_activeEcho);

            _data.Clear();
            State = EchoState.Idle;
            Debug.Log("[Echo] Rebobinado. Grabación descartada.");
        }

        private void OnPlayerInteracted(Interactables.IInteractable interactable)
        {
            if (State != EchoState.Recording) return;

            _data.interactions.Add(new InteractionEvent
            {
                time = _recordingTimer,
                target = interactable
            });
        }

        private void StartRecording()
        {
            _data.Clear();
            _data.startPosition = transform.position;
            _recordingTimer = 0f;
            State = EchoState.Recording;
            Debug.Log("[Echo] Grabación iniciada.");
        }

        private void StopRecording()
        {
            transform.position = _data.startPosition;
            _rb.linearVelocity = Vector2.zero;
            State = EchoState.Recorded;
            Debug.Log($"[Echo] Grabación guardada. Frames: {_data.frames.Count} | Duración: {_recordingTimer:F2}s");
        }

        public void OnEchoFinished()
        {
            State = EchoState.Idle;
        }
    }
}
