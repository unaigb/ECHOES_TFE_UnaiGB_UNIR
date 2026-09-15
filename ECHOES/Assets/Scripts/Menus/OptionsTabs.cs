using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace Echoes.Menus
{
    // Pestañas simples entre "General" (audio/pantalla) y "Controls". El botón de la pestaña
    // activa se desactiva (Interactable = false) para marcar "estás aquí". Además, por si el
    // Sprite Swap del Button no cambia nada visualmente (Transition mal puesto, Target Graphic
    // apuntando a otra imagen, etc.), esta clase también puede cambiar el sprite DIRECTAMENTE
    // por código, sin depender de la configuración del Button — rellena 'Tab Active/Inactive
    // Sprite' e 'Image' de cada pestaña más abajo si quieres esta vía garantizada.
    //
    // L1/R1 del mando cambian de pestaña directamente, sin tener que navegar hasta los botones.
    public class OptionsTabs : MonoBehaviour
    {
        [SerializeField] private GameObject generalPanel;
        [SerializeField] private GameObject controlsPanel;
        [SerializeField] private Button generalTabButton;
        [SerializeField] private Button controlsTabButton;

        [Tooltip("Primer control seleccionado al mostrar la pestaña General (normalmente el slider de Master).")]
        [SerializeField] private GameObject generalFirstSelected;
        [Tooltip("Primer control seleccionado al mostrar la pestaña Controls (opcional, no tiene por qué haber nada interactivo ahí).")]
        [SerializeField] private GameObject controlsFirstSelected;

        [Header("Alternativa garantizada al Sprite Swap del Button (opcional)")]
        [Tooltip("Deja estos 4 campos vacíos si el Sprite Swap del Button ya te funciona. Si no, rellénalos y el cambio de sprite se hace por código, sin depender del Button.")]
        [SerializeField] private Image generalTabImage;
        [SerializeField] private Image controlsTabImage;
        [SerializeField] private Sprite activeTabSprite;
        [SerializeField] private Sprite inactiveTabSprite;

        // Objetivo por defecto de la pestaña activa — para poder recuperar la selección si se
        // pierde (ver Update), sin tener que recordar aparte en qué pestaña estamos.
        private GameObject _currentFirstSelected;

        // Pon este componente en el mismo GameObject que OptionsMenu: así, cada vez que
        // OptionsMenu.Show() reactive el panel, se vuelve a abrir siempre en "General".
        private void OnEnable() => ShowGeneral();

        private void Update()
        {
            // Un clic en un hueco vacío de Opciones (fuera de sliders/toggle/botones) deselecciona
            // el EventSystem — igual que en el menú principal, sin nada seleccionado ni el teclado
            // ni el mando pueden navegar. Se restaura el primer control de la pestaña activa.
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null)
                Select(_currentFirstSelected);

            Gamepad gp = Gamepad.current;
            if (gp == null) return;
            if (gp.leftShoulder.wasPressedThisFrame) ShowGeneral();
            else if (gp.rightShoulder.wasPressedThisFrame) ShowControls();
        }

        public void ShowGeneral()
        {
            if (generalPanel != null) generalPanel.SetActive(true);
            if (controlsPanel != null) controlsPanel.SetActive(false);
            if (generalTabButton != null) generalTabButton.interactable = false;
            if (controlsTabButton != null) controlsTabButton.interactable = true;
            if (generalTabImage != null) generalTabImage.sprite = activeTabSprite;
            if (controlsTabImage != null) controlsTabImage.sprite = inactiveTabSprite;
            _currentFirstSelected = generalFirstSelected;
            Select(generalFirstSelected);
        }

        public void ShowControls()
        {
            if (generalPanel != null) generalPanel.SetActive(false);
            if (controlsPanel != null) controlsPanel.SetActive(true);
            if (generalTabButton != null) generalTabButton.interactable = true;
            if (controlsTabButton != null) controlsTabButton.interactable = false;
            if (generalTabImage != null) generalTabImage.sprite = inactiveTabSprite;
            if (controlsTabImage != null) controlsTabImage.sprite = activeTabSprite;
            _currentFirstSelected = controlsFirstSelected;
            Select(controlsFirstSelected);
        }

        private void Select(GameObject target)
        {
            if (target == null || EventSystem.current == null) return;
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(target);
        }
    }
}
