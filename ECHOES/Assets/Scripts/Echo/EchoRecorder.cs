using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Echoes.Audio;
using Echoes.Interactables;
using Echoes.Managers;

namespace Echoes.Echo
{
    public class EchoRecorder : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float maxRecordingDuration = 15f;
        [SerializeField] private GameObject echoPrefab;

        [Header("Sonido")]
        [SerializeField] private AudioClip recordStartSfx;
        [Range(0f, 5f)] [SerializeField] private float recordStartSfxVolume = 1f;
        [SerializeField] private AudioClip recordStopSfx;
        [Range(0f, 5f)] [SerializeField] private float recordStopSfxVolume = 1f;
        [SerializeField] private AudioClip deploySfx;
        [Range(0f, 5f)] [SerializeField] private float deploySfxVolume = 1f;
        [SerializeField] private AudioClip rewindSfx;
        [Range(0f, 5f)] [SerializeField] private float rewindSfxVolume = 1f;

        public EchoState State { get; private set; } = EchoState.Idle;
        public bool IsUnlocked { get; private set; } = false;
        // Para el sistema de guardado: se dispara la primera vez que se desbloquea el eco.
        public event Action OnUnlocked;
        public float RecordingProgress => maxRecordingDuration > 0 ? _recordingTimer / maxRecordingDuration : 0f;
        public Transform ActiveEchoTransform => _activeEcho != null ? _activeEcho.transform : null;

        private RecordingData _data = new();
        private float _recordingTimer;
        private Rigidbody2D _rb;
        private Player.PlayerInteraction _playerInteraction;
        private Player.PlayerInputHandler _input;
        private InputAction _recordAction;
        private InputAction _deployAction;
        private InputAction _rewindAction;
        private GameObject _activeEcho;
        // Límites de cámara vigentes al empezar a grabar — ver StopRecording().
        private RoomBoundary[] _roomExitsAtRecordStart;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _playerInteraction = GetComponent<Player.PlayerInteraction>();
            _input = GetComponent<Player.PlayerInputHandler>();
            _recordAction = InputSystem.actions.FindAction("Player/Record");
            _deployAction = InputSystem.actions.FindAction("Player/DeployEcho");
            _rewindAction = InputSystem.actions.FindAction("Player/Rewind");
        }

        // A diferencia de MoveInput (que PlayerInputHandler ya filtra solo), estas tres acciones
        // se escuchan aquí directamente por callback — sin este chequeo, InputEnabled=false
        // durante un diálogo o una cinemática no impedía grabar/desplegar/rebobinar ecos.
        private bool InputBlocked => _input != null && !_input.InputEnabled;

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

        public void Unlock()
        {
            if (IsUnlocked) return;
            IsUnlocked = true;
            OnUnlocked?.Invoke();
        }

        private void OnRecord(InputAction.CallbackContext ctx)
        {
            if (InputBlocked || !IsUnlocked) return;
            // Con una grabación ya guardada (Recorded) o un eco reproduciéndose (Playing), Record
            // no hace nada — antes se podía volver a grabar encima y se perdía la anterior sin
            // querer; ahora hay que rebobinar primero para descartarla a propósito.
            if (State == EchoState.Idle)
                StartRecording();
            else if (State == EchoState.Recording)
                StopRecording();
        }

        private void OnDeploy(InputAction.CallbackContext ctx)
        {
            if (InputBlocked || !IsUnlocked) return;
            if (State != EchoState.Recorded) return;

            if (_activeEcho != null)
                Destroy(_activeEcho);

            _activeEcho = Instantiate(echoPrefab, _data.startPosition, Quaternion.identity);
            _activeEcho.GetComponent<EchoPlayback>().Play(_data);

            State = EchoState.Playing;
            AudioManager.Instance?.PlaySfx(deploySfx, deploySfxVolume);
        }

        private void OnRewind(InputAction.CallbackContext ctx)
        {
            if (InputBlocked || !IsUnlocked) return;
            if (State == EchoState.Idle) return;

            if (_activeEcho != null)
                Destroy(_activeEcho);

            _data.Clear();
            State = EchoState.Idle;
            AudioManager.Instance?.PlaySfx(rewindSfx, rewindSfxVolume);
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
            _roomExitsAtRecordStart = LevelManager.Instance?.CurrentExits;
            AudioManager.Instance?.PlaySfx(recordStartSfx, recordStartSfxVolume);
            Debug.Log("[Echo] Grabación iniciada.");
        }

        private void StopRecording()
        {
            transform.position = _data.startPosition;
            _rb.linearVelocity = Vector2.zero;
            // Si durante la grabación el jugador cruzó de vuelta a la sala anterior (posible en
            // puertas con Previous Room Exits), este teletransporte a la posición de inicio no
            // pasa por ningún RoomBoundary — sin esto, la cámara se quedaría con los límites de
            // la sala de la que se volvió, aunque el jugador ya esté de vuelta en la de antes.
            if (_roomExitsAtRecordStart != null)
                LevelManager.Instance?.ReapplyExits(_roomExitsAtRecordStart);
            AudioManager.Instance?.PlaySfx(recordStopSfx, recordStopSfxVolume);
            State = EchoState.Recorded;
            Debug.Log($"[Echo] Grabación guardada. Frames: {_data.frames.Count} | Duración: {_recordingTimer:F2}s");
        }

        public void OnEchoFinished()
        {
            State = EchoState.Idle;
        }
    }
}
