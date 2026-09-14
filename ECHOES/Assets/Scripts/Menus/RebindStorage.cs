using UnityEngine;
using UnityEngine.InputSystem;

namespace Echoes.Menus
{
    // Persiste los overrides de remapeo de teclas/mando en PlayerPrefs (igual que el resto del
    // guardado del proyecto) y los recarga solo al arrancar, antes de que nada — InputHints,
    // ControlsDisplay, el propio jugador— pida un binding.
    public static class RebindStorage
    {
        private const string PrefsKey = "Echoes.InputBindingOverrides";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Load()
        {
            if (InputSystem.actions == null) return;
            string json = PlayerPrefs.GetString(PrefsKey, string.Empty);
            if (!string.IsNullOrEmpty(json))
                InputSystem.actions.LoadBindingOverridesFromJson(json);
        }

        public static void Save()
        {
            if (InputSystem.actions == null) return;
            PlayerPrefs.SetString(PrefsKey, InputSystem.actions.SaveBindingOverridesAsJson());
        }

        // Botón "restaurar valores por defecto" de Opciones.
        public static void ResetToDefaults()
        {
            if (InputSystem.actions == null) return;
            InputSystem.actions.RemoveAllBindingOverrides();
            PlayerPrefs.DeleteKey(PrefsKey);
        }
    }
}
