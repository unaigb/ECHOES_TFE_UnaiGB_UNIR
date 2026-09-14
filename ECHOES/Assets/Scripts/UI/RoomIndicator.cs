using UnityEngine;
using TMPro;
using Echoes.Managers;

namespace Echoes.UI
{
    public class RoomIndicator : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI label;

        // OnEnable, no Start: este objeto cuelga de LocationBox, que PauseMenu apaga/enciende
        // con SetActive() en cada pausa — Start() solo corre una vez en toda la vida del objeto,
        // así que tras la primera pausa se quedaba desuscrito para siempre (el rótulo se
        // congelaba con el nombre de la última sala vista antes de esa pausa).
        private void OnEnable()
        {
            if (LevelManager.Instance == null) return;
            LevelManager.Instance.OnRoomChanged += SetRoomName;

            if (!string.IsNullOrEmpty(LevelManager.Instance.CurrentRoomName))
                SetRoomName(LevelManager.Instance.CurrentRoomName);
        }

        private void OnDisable()
        {
            if (LevelManager.Instance != null)
                LevelManager.Instance.OnRoomChanged -= SetRoomName;
        }

        private void SetRoomName(string roomName)
        {
            label.text = roomName;
        }
    }
}
