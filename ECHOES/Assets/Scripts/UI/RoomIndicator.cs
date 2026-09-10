using UnityEngine;
using TMPro;
using Echoes.Managers;

namespace Echoes.UI
{
    public class RoomIndicator : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI label;

        private void Start()
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
