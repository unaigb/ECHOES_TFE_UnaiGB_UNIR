using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Echoes.Menus;

namespace Echoes.Audio
{
    // Sonidos de navegación de UI (mover selección / confirmar / cancelar), autocontenido — no
    // depende de AudioManager (que solo existe en la escena de juego) para poder vivir también
    // en Title. Uno de estos por escena, con su propio AudioSource; pon los mismos tres clips en
    // los dos si quieres que suenen igual en Título y en Pausa/Opciones.
    public class UISoundManager : MonoBehaviour
    {
        public static UISoundManager Instance { get; private set; }

        [SerializeField] private AudioSource sfxSource;
        [Tooltip("Al mover la selección con teclado/mando (flechas, stick, D-pad).")]
        [SerializeField] private AudioClip navigateSfx;
        [Range(0f, 5f)] [SerializeField] private float navigateSfxVolume = 1f;
        [Tooltip("Al confirmar/entrar en una opción: UI/Submit (teclado/mando) o clic sobre cualquier elemento de UI.")]
        [SerializeField] private AudioClip confirmSfx;
        [Range(0f, 5f)] [SerializeField] private float confirmSfxVolume = 1f;
        [Tooltip("Al cancelar/volver atrás: UI/Cancel (incluye Esc en teclado).")]
        [SerializeField] private AudioClip cancelSfx;
        [Range(0f, 5f)] [SerializeField] private float cancelSfxVolume = 1f;

        private InputAction _submitAction;
        private InputAction _cancelAction;
        private GameObject _lastSelected;

        private void Awake()
        {
            Instance = this;
            _submitAction = InputSystem.actions.FindAction("UI/Submit");
            _cancelAction = InputSystem.actions.FindAction("UI/Cancel");
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable()
        {
            if (_submitAction != null) _submitAction.performed += OnSubmit;
            if (_cancelAction != null) _cancelAction.performed += OnCancel;
        }

        private void OnDisable()
        {
            if (_submitAction != null) _submitAction.performed -= OnSubmit;
            if (_cancelAction != null) _cancelAction.performed -= OnCancel;
        }

        private void Update()
        {
            GameObject current = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (current != null && current != _lastSelected) Play(navigateSfx, navigateSfxVolume);
            _lastSelected = current;

            // UI/Submit no salta con clic de ratón — se comprueba aparte, mirando si el puntero
            // estaba sobre algún elemento de UI en el momento del clic. Igual que en
            // OnSubmit/OnCancel, exige que haya algo seleccionado — si no, un clic sobre
            // cualquier gráfico del HUD con Raycast Target sonaría igual en mitad de la partida.
            if (current != null && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame
                && EventSystem.current.IsPointerOverGameObject())
                Play(confirmSfx, confirmSfxVolume);
        }

        // UI/Submit y UI/Cancel están activos SIEMPRE, también durante la partida (los usan
        // diálogos y pausa) — sin este chequeo, el mismo botón A/B del mando que usas para jugar
        // hacía sonar el confirmar/cancelar de menú aunque no hubiera ningún menú abierto. Con
        // algo seleccionado en el EventSystem es buena señal de que sí estamos en un menú —
        // siempre que se limpie la selección al volver al juego (ver PauseMenu.Resume).
        private bool InMenuContext => EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null;

        private void OnSubmit(InputAction.CallbackContext ctx) { if (InMenuContext) Play(confirmSfx, confirmSfxVolume); }
        private void OnCancel(InputAction.CallbackContext ctx) { if (InMenuContext) Play(cancelSfx, cancelSfxVolume); }

        // Para sonidos puntuales de un menú concreto que no encajan como "navegar/confirmar/
        // cancelar" genéricos (p. ej. TitleScreen: revelar el menú, o entrar de verdad a la
        // partida) — reutiliza el mismo AudioSource en vez de necesitar uno propio.
        public void PlayCustom(AudioClip clip, float volumeScale = 1f) => Play(clip, volumeScale);

        private void Play(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null || sfxSource == null) return;
            sfxSource.PlayOneShot(clip, PlayerPrefs.GetFloat(OptionsMenu.SfxVolumeKey, 1f) * volumeScale);
        }
    }
}
