using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using Echoes.Audio;

namespace Echoes.Menus
{
    // Panel de Opciones compartido entre el título y la pausa. Show(caller) recuerda qué panel
    // lo abrió (se pasa como parámetro estático del botón en el Inspector) para que Hide()
    // pueda volver a mostrarlo — así un único panel de Opciones vale para los dos sitios.
    //
    // Volumen: Master se aplica ya mismo (AudioListener.volume). Music/Sfx se guardan en
    // PlayerPrefs con estas claves — AudioManager las lee ahí para el SFX, y Music además se
    // empuja en vivo (ver SetupVolume) para que se note el cambio en la música ya sonando.
    public class OptionsMenu : MonoBehaviour
    {
        public const string MusicVolumeKey = "Echoes.Volume.Music";
        public const string SfxVolumeKey = "Echoes.Volume.Sfx";
        private const string MasterVolumeKey = "Echoes.Volume.Master";

        [Header("Audio")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;

        [Header("Pantalla")]
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private TMP_Dropdown resolutionDropdown;

        [Header("Controles")]
        [Tooltip("La misma pestaña Controls que gestiona el remapeo — para no cerrar el panel entero con el mismo Escape que cancela un remapeo en curso.")]
        [SerializeField] private ControlsDisplay controlsDisplay;

        private Resolution[] _resolutions;
        private InputAction _cancelAction;

        private void Awake()
        {
            // UI/Cancel (no un botón posicional) — así se resuelve solo según la marca del
            // mando conectado, en vez de asumir que "Sur" o "Este" significa cancelar siempre.
            _cancelAction = InputSystem.actions.FindAction("UI/Cancel");
        }

        private void Start()
        {
            SetupVolume();
            SetupScreen();
        }

        private void SetupVolume()
        {
            float master = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
            float music = PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
            float sfx = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);

            ApplyMasterVolume(master);
            if (masterVolumeSlider != null) { masterVolumeSlider.value = master; masterVolumeSlider.onValueChanged.AddListener(ApplyMasterVolume); }
            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.value = music;
                // Uno de los dos será null según la escena (AudioManager solo en TrainingCourt,
                // TitleMusic solo en Title) — ambas llamadas son inofensivas si no aplican.
                musicVolumeSlider.onValueChanged.AddListener(v =>
                {
                    PlayerPrefs.SetFloat(MusicVolumeKey, v);
                    AudioManager.Instance?.SetMusicVolume(v);
                    TitleMusic.Instance?.SetVolume(v);
                });
            }
            if (sfxVolumeSlider != null) { sfxVolumeSlider.value = sfx; sfxVolumeSlider.onValueChanged.AddListener(v => PlayerPrefs.SetFloat(SfxVolumeKey, v)); }
        }

        private void ApplyMasterVolume(float value)
        {
            AudioListener.volume = value;
            PlayerPrefs.SetFloat(MasterVolumeKey, value);
        }

        private void SetupScreen()
        {
            if (fullscreenToggle != null)
            {
                fullscreenToggle.isOn = Screen.fullScreen;
                fullscreenToggle.onValueChanged.AddListener(v => Screen.fullScreen = v);
            }

            if (resolutionDropdown == null) return;

            _resolutions = Screen.resolutions;
            var options = new List<string>();
            int currentIndex = 0;
            for (int i = 0; i < _resolutions.Length; i++)
            {
                options.Add($"{_resolutions[i].width} x {_resolutions[i].height}");
                if (_resolutions[i].width == Screen.width && _resolutions[i].height == Screen.height)
                    currentIndex = i;
            }
            resolutionDropdown.ClearOptions();
            resolutionDropdown.AddOptions(options);
            resolutionDropdown.value = currentIndex;
            resolutionDropdown.RefreshShownValue();
            resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        }

        private void OnResolutionChanged(int index)
        {
            Resolution r = _resolutions[index];
            Screen.SetResolution(r.width, r.height, Screen.fullScreen);
        }

        private GameObject _callerPanel;

        // El parámetro se asigna en el Inspector como "static parameter" del botón que abre
        // Opciones (arrastra ahí el panel de Título o el de Pausa, según desde dónde se abra).
        public void Show(GameObject callerPanel)
        {
            _callerPanel = callerPanel;
            if (_callerPanel != null) _callerPanel.SetActive(false);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            if (_callerPanel == null) return;

            _callerPanel.SetActive(true);

            // Al desactivar este panel, Unity deselecciona lo que hubiera dentro y nadie elige
            // uno nuevo — sin esto, el mando se queda sin poder navegar hasta la próxima vez que
            // el GamepadSelectionGuard del panel llamador reaccione por su cuenta.
            GamepadSelectionGuard guard = _callerPanel.GetComponent<GamepadSelectionGuard>();
            if (guard != null) guard.SelectFallbackNow();
        }

        // Se cierra solo con Esc/P o UI/Cancel del mando — válido tanto si se abrió desde el
        // título como desde la pausa, sin que ninguno de los dos tenga que saber nada de esto
        // (evita el bug de que Esc reanudara la partida con Opciones aún en pantalla).
        private void Update()
        {
            // Si el desplegable de resolución está abierto, que TMP_Dropdown gestione su propio
            // Cancel primero — si aquí también reaccionamos al mismo Cancel el mismo frame,
            // desactivamos el panel por debajo de su lista a medio animar y revienta con NRE.
            if (resolutionDropdown != null && resolutionDropdown.IsExpanded) return;
            if (controlsDisplay != null && controlsDisplay.IsRebinding) return;

            bool back = (Keyboard.current != null && (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.pKey.wasPressedThisFrame))
                || (_cancelAction != null && _cancelAction.WasPressedThisFrame());
            if (back) Hide();
        }
    }
}
