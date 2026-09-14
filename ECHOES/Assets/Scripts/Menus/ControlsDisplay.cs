using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Echoes.Dialogue; // InputHints vive ahí (helper compartido, ver DialogueManager)

namespace Echoes.Menus
{
    // Pestaña "Controls" de Opciones: acción + su tecla/botón actual, con remapeo opcional por
    // fila. Cada fila remapea SIEMPRE el binding del dispositivo que se esté usando ahora mismo
    // (InputHints.UsingGamepad) — no hace falta una columna separada de teclado y de mando, el
    // propio InputHints ya sabe con qué dispositivo se está jugando.
    //
    // No soporta remapear "Move" (WASD / stick): es una acción compuesta con varias partes de
    // teclado, y no tiene sentido un solo botón de "remapear" para eso. Deja rebindButton sin
    // asignar en esa fila y esta clase se la salta sin más.
    public class ControlsDisplay : MonoBehaviour
    {
        [System.Serializable]
        public class Row
        {
            [Tooltip("Nombre de la acción tal cual en el Input Actions asset, p.ej. \"Player/Interact\".")]
            public string actionName;
            public TextMeshProUGUI keyLabel;
            [Tooltip("Opcional: botón para remapear esta fila. Déjalo vacío en filas no remapeables (Move).")]
            public Button rebindButton;
        }

        [SerializeField] private Row[] rows;
        [Tooltip("Texto que se muestra en el keyLabel de la fila mientras se espera la pulsación.")]
        [SerializeField] private string waitingText = "Press any key...";

        private InputActionRebindingExtensions.RebindingOperation _activeRebind;

        // OptionsMenu lo consulta para no cerrarse a la vez que Escape cancela un remapeo en
        // curso — mismo motivo por el que ya mira resolutionDropdown.IsExpanded.
        public bool IsRebinding => _activeRebind != null;

        private void OnEnable() => Refresh();

        private void OnDisable() => _activeRebind?.Cancel();

        public void Refresh()
        {
            if (rows == null) return;
            foreach (var row in rows)
            {
                if (row.keyLabel != null)
                    row.keyLabel.text = InputHints.Key(row.actionName, useOverrides: false);

                if (row.rebindButton != null)
                {
                    row.rebindButton.onClick.RemoveAllListeners();
                    Row capturedRow = row;
                    row.rebindButton.onClick.AddListener(() => StartRebind(capturedRow));
                }
            }
        }

        // Botón "Reset to Default" de la pestaña Controls.
        public void ResetToDefaults()
        {
            RebindStorage.ResetToDefaults();
            Refresh();
        }

        private void StartRebind(Row row)
        {
            if (_activeRebind != null) return; // ya hay un remapeo en curso

            InputAction action = InputSystem.actions != null ? InputSystem.actions.FindAction(row.actionName) : null;
            if (action == null) return;

            int bindingIndex = FindBindingIndex(action, InputHints.UsingGamepad);
            if (bindingIndex < 0) return;

            if (row.keyLabel != null) row.keyLabel.text = waitingText;
            SetRowInteractable(row, false);

            bool wasEnabled = action.enabled;
            if (wasEnabled) action.Disable();

            _activeRebind = action.PerformInteractiveRebinding(bindingIndex)
                .WithControlsExcluding("<Mouse>/position")
                .WithControlsExcluding("<Mouse>/delta")
                .WithCancelingThrough("<Keyboard>/escape")
                .OnMatchWaitForAnother(0.1f)
                .OnComplete(_ => FinishRebind(row, action, wasEnabled, true))
                .OnCancel(_ => FinishRebind(row, action, wasEnabled, false))
                .Start();
        }

        private void FinishRebind(Row row, InputAction action, bool wasEnabled, bool completed)
        {
            _activeRebind?.Dispose();
            _activeRebind = null;

            if (wasEnabled) action.Enable();
            SetRowInteractable(row, true);

            // Al desactivar el botón durante la espera, el EventSystem pierde la selección y no
            // la recupera solo — con mando, hacía falta cambiar de pestaña o reabrir Opciones
            // para volver a navegar. Se reselecciona el mismo botón, complete o no el remapeo.
            if (row.rebindButton != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(row.rebindButton.gameObject);

            if (completed) RebindStorage.Save();
            Refresh();
        }

        private static void SetRowInteractable(Row row, bool interactable)
        {
            if (row.rebindButton != null) row.rebindButton.interactable = interactable;
        }

        // Misma lógica que InputHints.DisplayFor: se mira effectivePath (no el grupo, que en
        // varios bindings de este asset viene vacío) para decidir de qué dispositivo es.
        private static int FindBindingIndex(InputAction action, bool gamepad)
        {
            for (int i = 0; i < action.bindings.Count; i++)
            {
                InputBinding b = action.bindings[i];
                if (b.isComposite || b.isPartOfComposite) continue;

                string path = b.effectivePath ?? string.Empty;
                bool isGamepad = path.Contains("Gamepad") || path.Contains("Joystick") || path.Contains("XR");
                bool isKeyboard = path.Contains("Keyboard") || path.Contains("Mouse") || path.Contains("Pointer");

                if ((gamepad && isGamepad) || (!gamepad && isKeyboard)) return i;
            }
            return -1;
        }
    }
}
