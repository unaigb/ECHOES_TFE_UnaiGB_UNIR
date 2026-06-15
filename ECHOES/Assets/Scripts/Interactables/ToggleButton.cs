using UnityEngine;

namespace Echoes.Interactables
{
    public class ToggleButton : MonoBehaviour, IInteractable
    {
        [SerializeField] private Door targetDoor;

        private bool _activated = false;

        public void Interact()
        {
            if (targetDoor == null) return;
            _activated = !_activated;
            targetDoor.Toggle();
            Debug.Log($"[Button] {gameObject.name} → {(_activated ? "ON" : "OFF")}");
        }
    }
}
