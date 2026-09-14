using UnityEngine;
using Echoes.Echo;
using Echoes.Player;
using Echoes.Managers;

namespace Echoes.Saving
{
    // Autoguarda cada vez que cambia de sala o se desbloquea el eco, y aplica el guardado
    // cuando el jugador pulsa "Continue" en el título.
    public class SaveManager : MonoBehaviour
    {
        [SerializeField] private EchoRecorder echoRecorder;
        [SerializeField] private PlayerAnimator playerAnimator;

        private void Start()
        {
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.OnRoomChanged += OnRoomChanged;
                // La sala inicial se aplica en LevelManager.Awake(), antes de que este Start()
                // llegue a suscribirse — sin esto, no existiría guardado si detectan al jugador
                // en la primera sala antes de cruzar a la siguiente. El HasSave() es importante:
                // sin él, en una recarga por detección esto podría pisar el guardado real con
                // "Sala 01" antes de que GameEntryPoint llegue a leerlo (orden de Start() entre
                // objetos no garantizado por Unity).
                if (!string.IsNullOrEmpty(LevelManager.Instance.CurrentRoomName) && !SaveSystem.HasSave())
                    Persist(LevelManager.Instance.CurrentRoomName);
            }
            if (echoRecorder != null)
                echoRecorder.OnUnlocked += OnEchoUnlocked;
        }

        private void OnDisable()
        {
            if (LevelManager.Instance != null)
                LevelManager.Instance.OnRoomChanged -= OnRoomChanged;
            if (echoRecorder != null)
                echoRecorder.OnUnlocked -= OnEchoUnlocked;
        }

        private void OnRoomChanged(string roomName) => Persist(roomName);

        private void OnEchoUnlocked()
        {
            if (LevelManager.Instance != null) Persist(LevelManager.Instance.CurrentRoomName);
        }

        private void Persist(string roomName)
        {
            SaveSystem.Save(new SaveData
            {
                roomName = roomName,
                echoUnlocked = echoRecorder != null && echoRecorder.IsUnlocked
            });
        }

        // Llamado por TitleScreen.OnContinue().
        public void ApplyLoadedSave()
        {
            SaveData data = SaveSystem.Load();
            if (data == null) return;

            if (!string.IsNullOrEmpty(data.roomName) && LevelManager.Instance != null)
                LevelManager.Instance.TryLoadRoom(data.roomName);

            if (data.echoUnlocked && echoRecorder != null)
            {
                echoRecorder.Unlock();
                if (playerAnimator != null) playerAnimator.SetEcoUnlocked(true);
            }
        }
    }
}
