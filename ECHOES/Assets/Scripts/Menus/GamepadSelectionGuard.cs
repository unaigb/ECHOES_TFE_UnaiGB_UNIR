using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Echoes.Menus
{
    // El ratón deselecciona el control con el que interactúa al terminar (soltar el slider,
    // etc.). Si después el jugador vuelve al mando, no hay nada seleccionado y las flechas/stick
    // no mueven nada hasta que algo se seleccione a mano otra vez. Este componente vigila: si no
    // hay nada seleccionado y detecta actividad del mando, selecciona un control por defecto.
    //
    // Pon uno de estos en cada panel navegable (MenuPanel, GeneralPanel de Opciones, PausePanel...)
    // con su propio "de vuelta a qué control".
    public class GamepadSelectionGuard : MonoBehaviour
    {
        [SerializeField] private GameObject fallbackSelected;

        private void Update()
        {
            if (EventSystem.current == null) return;
            if (EventSystem.current.currentSelectedGameObject != null) return;
            if (fallbackSelected == null || !fallbackSelected.activeInHierarchy) return;
            if (!GamepadActivityThisFrame()) return;

            EventSystem.current.SetSelectedGameObject(fallbackSelected);
        }

        // Para cuando algo más (OptionsMenu.Hide(), por ejemplo) ya sabe que la selección se
        // acaba de perder y quiere recuperarla YA, sin esperar a la próxima actividad del mando.
        public void SelectFallbackNow()
        {
            if (fallbackSelected != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(fallbackSelected);
        }

        private bool GamepadActivityThisFrame()
        {
            Gamepad gp = Gamepad.current;
            if (gp == null) return false;
            return gp.leftStick.ReadValue().sqrMagnitude > 0.2f
                || gp.dpad.ReadValue().sqrMagnitude > 0.2f
                || gp.buttonSouth.wasPressedThisFrame
                || gp.buttonEast.wasPressedThisFrame;
        }
    }
}
