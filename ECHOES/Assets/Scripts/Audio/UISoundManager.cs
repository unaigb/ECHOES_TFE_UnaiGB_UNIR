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
    //
    // DefaultExecutionOrder alto a propósito: el EventSystem procesa el clic (selección +
    // Submit) dentro de su propio Update, en el orden por defecto. Sin esto, el Update() de
    // aquí podía ejecutarse ANTES en el mismo frame y leer todavía la selección vieja —
    // currentSelectedGameObject y "se ha hecho clic" quedaban desincronizados un frame entre
    // sí, así que el chequeo de "o lo uno o lo otro" de más abajo no siempre pillaba los dos a
    // la vez y sonaban los dos golpes solapados en frames consecutivos.
    [DefaultExecutionOrder(1000)]
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
        private float _lastMouseConfirmTime = -1f;

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

            // UI/Submit no salta con clic de ratón — se comprueba aparte, mirando si el puntero
            // estaba sobre algún elemento de UI en el momento del clic. Igual que en
            // OnSubmit/OnCancel, exige que haya algo seleccionado — si no, un clic sobre
            // cualquier gráfico del HUD con Raycast Target sonaría igual en mitad de la partida.
            bool mouseConfirm = current != null && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame
                && EventSystem.current.IsPointerOverGameObject();

            // Un clic de ratón sobre un botón distinto al ya seleccionado cambia la selección Y
            // confirma casi en el mismo instante — sin este "o lo uno o lo otro", sonaban los
            // dos golpes solapados. Se usa una pequeña ventana de tiempo real (no solo "el mismo
            // Update") porque el EventSystem puede resolver la selección y el clic en pasadas de
            // frame distintas según el dispositivo de puntero — con un simple chequeo del mismo
            // frame, a veces se colaban los dos igual. El teclado/mando sí separan ambos momentos
            // de verdad (Navigate primero, Submit después, sin ratón de por medio), así que ahí
            // el sonido de navegar sigue sonando solo, sin verse afectado por esta ventana.
            if (mouseConfirm)
            {
                _lastMouseConfirmTime = Time.unscaledTime;
                PlayConfirmOrBack(current);
            }
            else if (current != null && current != _lastSelected && Time.unscaledTime - _lastMouseConfirmTime > 0.15f)
            {
                Play(navigateSfx, navigateSfxVolume);
            }

            _lastSelected = current;
        }

        // UI/Submit y UI/Cancel están activos SIEMPRE, también durante la partida (los usan
        // diálogos y pausa) — sin este chequeo, el mismo botón A/B del mando que usas para jugar
        // hacía sonar el confirmar/cancelar de menú aunque no hubiera ningún menú abierto. Con
        // algo seleccionado en el EventSystem es buena señal de que sí estamos en un menú —
        // siempre que se limpie la selección al volver al juego (ver PauseMenu.Resume).
        private bool InMenuContext => EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null;

        private void OnSubmit(InputAction.CallbackContext ctx)
        {
            if (InMenuContext) PlayConfirmOrBack(EventSystem.current.currentSelectedGameObject);
        }
        private void OnCancel(InputAction.CallbackContext ctx) { if (InMenuContext) Play(cancelSfx, cancelSfxVolume); }

        // Un botón puede ser un "Submit" a efectos de Unity (confirma la selección) pero
        // significar "volver" a efectos de diseño (p. ej. el Volver de Opciones) — con
        // BackButtonSound puesto en él, suena el mismo clip que Cancel en vez del de Confirm
        // genérico, para que el sonido coincida con lo que el botón hace de verdad.
        private void PlayConfirmOrBack(GameObject target)
        {
            bool isBack = target != null && target.GetComponent<BackButtonSound>() != null;
            if (isBack) Play(cancelSfx, cancelSfxVolume);
            else Play(confirmSfx, confirmSfxVolume);
        }

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
