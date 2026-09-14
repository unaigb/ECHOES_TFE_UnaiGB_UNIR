using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Echoes.Dialogue
{
    // Traduce una acción del Input System al nombre real de su tecla/botón, según el último
    // dispositivo que se haya usado (teclado o mando). Se auto-inicializa, no hay que montar
    // nada en el Editor. Si más adelante existe un remapeo de controles, los overrides de
    // rebind se reflejan solos porque tiran de GetBindingDisplayString().
    //
    // Uso: InputHints.Key("Player/Move")  ->  "W/A/S/D"  o  "Left Stick"  según el caso.
    public static class InputHints
    {
        public static bool UsingGamepad { get; private set; }

        // Frases más naturales para casos que quedan feos en prosa (los sticks, sobre todo).
        // Clave: "<accion>:<gamepad|keyboard>". Si no hay entrada, se usa el nombre automático.
        // El texto del juego está en inglés.
        private static readonly Dictionary<string, string> Overrides = new Dictionary<string, string>
        {
            { "Move:keyboard", "W/A/S/D" },
            { "Move:gamepad",  "the left stick" },
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            UsingGamepad = Gamepad.current != null && Keyboard.current == null;
            InputSystem.onActionChange += OnActionChange;
        }

        private static void OnActionChange(object obj, InputActionChange change)
        {
            if (change != InputActionChange.ActionPerformed) return;
            if (!(obj is InputAction action)) return;

            InputDevice device = action.activeControl != null ? action.activeControl.device : null;
            if (device is Gamepad) UsingGamepad = true;
            else if (device is Keyboard || device is Mouse) UsingGamepad = false;
        }

        // useOverrides = false para listados tipo "Controls" de Opciones, donde interesa el
        // nombre real del dispositivo ("Left Stick") y no la frase de prosa para diálogos
        // ("the left stick" queda raro fuera de una frase).
        public static string Key(string actionName, bool useOverrides = true)
        {
            InputAction action = InputSystem.actions != null ? InputSystem.actions.FindAction(actionName) : null;
            if (action == null) return $"[{actionName}]";

            if (useOverrides)
            {
                // Nombre corto de la acción para buscar en Overrides ("Player/Move" -> "Move").
                int slash = actionName.LastIndexOf('/');
                string shortName = slash >= 0 ? actionName.Substring(slash + 1) : actionName;
                string overrideKey = shortName + (UsingGamepad ? ":gamepad" : ":keyboard");
                if (Overrides.TryGetValue(overrideKey, out string phrase)) return phrase;
            }

            return DisplayFor(action, UsingGamepad);
        }

        private static string DisplayFor(InputAction action, bool gamepad)
        {
            const InputBinding.DisplayStringOptions opts =
                InputBinding.DisplayStringOptions.DontIncludeInteractions;

            for (int i = 0; i < action.bindings.Count; i++)
            {
                InputBinding b = action.bindings[i];
                if (b.isPartOfComposite) continue;

                // Para un composite (p. ej. WASD), el path del encabezado no lleva dispositivo:
                // se mira la primera parte para decidir de qué dispositivo es. Se usa
                // effectivePath (no path) para que un remapeo futuro se refleje bien.
                string probe = b.effectivePath ?? string.Empty;
                if (b.isComposite && i + 1 < action.bindings.Count)
                    probe = action.bindings[i + 1].effectivePath ?? string.Empty;

                bool isGamepad = probe.Contains("Gamepad") || probe.Contains("Joystick") || probe.Contains("XR");
                bool isKeyboard = probe.Contains("Keyboard") || probe.Contains("Mouse") || probe.Contains("Pointer");

                if ((gamepad && isGamepad) || (!gamepad && isKeyboard))
                {
                    string s = action.GetBindingDisplayString(i, opts);
                    if (!string.IsNullOrEmpty(s)) return s;
                }
            }

            // Sin binding para ese dispositivo: lo que haya.
            return action.GetBindingDisplayString(opts);
        }
    }
}
