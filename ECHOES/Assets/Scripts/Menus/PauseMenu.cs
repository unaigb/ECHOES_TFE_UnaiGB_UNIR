using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Echoes.Player;
using Echoes.Dialogue;
using Echoes.Managers;
using Echoes.UI;
using Echoes.Saving;

namespace Echoes.Menus
{
    // Abrir pausa: Esc, P, o Start del mando (botón dedicado, igual en todas las marcas).
    // Cerrar/volver: Esc, P, o UI/Cancel del mando — Cancel es una acción semántica, no un
    // botón posicional, así que ya se resuelve solo en Xbox/PlayStation/Nintendo sin tener que
    // elegir "Sur" o "Este" a mano. No pausa mientras hay un diálogo de Ir1s en curso. Si
    // Opciones está abierto, es OptionsMenu quien gestiona su propio cierre — aquí simplemente
    // no se hace nada ese frame.
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private PlayerInputHandler playerInput;
        [Tooltip("Nombre exacto de la escena de título, tal cual aparece en Build Settings.")]
        [SerializeField] private string titleSceneName = "Title";
        [Tooltip("Panel del HUD con el nombre de la sala actual — se oculta mientras dura la pausa.")]
        [SerializeField] private GameObject locationBox;
        [Tooltip("El OptionsMenu de esta escena — mientras esté abierto, esta clase no hace nada (Options gestiona su propio Esc/back).")]
        [SerializeField] private OptionsMenu optionsMenu;
        [Tooltip("Botón seleccionado al abrir la pausa, para que el mando funcione sin haber usado antes el ratón. Normalmente Resume.")]
        [SerializeField] private GameObject firstSelectedButton;
        [Tooltip("El CanvasGroup del HUD (el mismo que usa IntroCutscene). Mientras la pausa está abierta se fuerza a alpha 1 para que el propio panel de pausa se vea aunque el HUD esté a 0 (p.ej. durante la cinemática) — se restaura exactamente al reanudar.")]
        [SerializeField] private CanvasGroup hudCanvasGroup;
        [Tooltip("El mismo AutosaveNotice que usa GameEntryPoint — Restart Room y Main Menu lo reutilizan para fundir a negro antes de cambiar de escena. Opcional: si se deja vacío, cambia de escena sin fundido previo.")]
        [SerializeField] private AutosaveNotice autosaveNotice;

        public bool IsPaused { get; private set; }
        // Compartido entre RestartRoom y GoToMainMenu: ambos cargan una escena tras un fundido,
        // así que uno solo basta para que no se puedan disparar los dos (o el mismo dos veces).
        private bool _transitioning;

        private InputAction _cancelAction;
        private bool _prevInputEnabled;
        private float _prevHudAlpha;
        private bool _prevHudInteractable;
        private bool _prevHudBlocksRaycasts;

        private void Awake()
        {
            _cancelAction = InputSystem.actions.FindAction("UI/Cancel");
            if (pausePanel != null) pausePanel.SetActive(false);
        }

        private void Update()
        {
            // Opciones abierto: que se cierre solo, no interferir.
            if (optionsMenu != null && optionsMenu.gameObject.activeSelf) return;

            if (!IsPaused)
            {
                if (OpenPressed() && CanPause()) Pause();
                return;
            }

            if (ClosePressed()) Resume();
        }

        private bool OpenPressed()
        {
            if (Keyboard.current != null && (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.pKey.wasPressedThisFrame))
                return true;
            return Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame;
        }

        private bool ClosePressed()
        {
            if (Keyboard.current != null && (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.pKey.wasPressedThisFrame))
                return true;
            return _cancelAction != null && _cancelAction.WasPressedThisFrame();
        }

        private bool CanPause()
        {
            if (DialogueManager.Instance != null && DialogueManager.Instance.IsActive) return false;
            // Con la pantalla en negro (aviso de autoguardado, o el fundido de una recarga) es
            // fácil pausar sin querer y no queda claro qué ha pasado — se bloquea hasta que se
            // ve algo.
            if (autosaveNotice != null && autosaveNotice.IsPlaying) return false;
            return true;
        }

        public void Pause()
        {
            IsPaused = true;
            _prevInputEnabled = playerInput.InputEnabled;
            playerInput.InputEnabled = false;
            Time.timeScale = 0f;
            Cursor.visible = true;
            if (pausePanel != null) pausePanel.SetActive(true);
            if (locationBox != null) locationBox.SetActive(false);

            if (hudCanvasGroup != null)
            {
                _prevHudAlpha = hudCanvasGroup.alpha;
                _prevHudInteractable = hudCanvasGroup.interactable;
                _prevHudBlocksRaycasts = hudCanvasGroup.blocksRaycasts;
                hudCanvasGroup.alpha = 1f;
                hudCanvasGroup.interactable = true;
                hudCanvasGroup.blocksRaycasts = true;
            }

            // Sin esto, el mando solo funciona si algo ya estaba seleccionado de antes (por
            // eso "a veces sí salía") — con un mando recién puesto a usar, no hay nada.
            if (firstSelectedButton != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(firstSelectedButton);
            }
        }

        public void Resume()
        {
            IsPaused = false;
            playerInput.InputEnabled = _prevInputEnabled;
            Time.timeScale = 1f;
            Cursor.visible = false;
            if (pausePanel != null) pausePanel.SetActive(false);
            if (locationBox != null) locationBox.SetActive(true);

            // Sin esto, el botón que quedó seleccionado en el menú de pausa (p.ej. "Resume")
            // se queda como currentSelectedGameObject del EventSystem para siempre, aunque el
            // panel ya esté oculto — UISoundManager lo usa como señal de "hay un menú abierto",
            // así que sin limpiarlo aquí, el propio A/B del mando durante la partida sonaría
            // como si estuvieras en un menú.
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);

            if (hudCanvasGroup != null)
            {
                hudCanvasGroup.alpha = _prevHudAlpha;
                hudCanvasGroup.interactable = _prevHudInteractable;
                hudCanvasGroup.blocksRaycasts = _prevHudBlocksRaycasts;
            }
        }

        // Mismo mecanismo que usa DetectionScreen al capturarte: recarga la escena reapareciendo
        // en la última sala guardada, por si hay que salir de un bug sin perder el progreso.
        // Funde a negro primero (como el flash de DetectionScreen, pero sin el aviso) — la
        // pantalla que aparece tras la recarga es la misma que hace este fundido, así que
        // continúa desde donde lo deja este, sin corte.
        public void RestartRoom()
        {
            if (_transitioning) return;
            _transitioning = true;
            Time.timeScale = 1f;

            if (autosaveNotice != null)
                autosaveNotice.FadeToBlack(DoRestart);
            else
                DoRestart();
        }

        private void DoRestart()
        {
            if (LevelManager.Instance != null) LevelManager.Instance.RestartLevel();
        }

        // Mismo fundido que Restart Room, antes de volver al Título — que a su vez nace ya en
        // negro y se retira con su propio fundido de vuelta (TitleScreen.RevealFromBlack), así
        // que la transición queda continua en los dos sentidos.
        public void GoToMainMenu()
        {
            if (_transitioning) return;
            _transitioning = true;
            Time.timeScale = 1f;

            if (autosaveNotice != null)
                autosaveNotice.FadeToBlack(DoGoToMainMenu);
            else
                DoGoToMainMenu();
        }

        private void DoGoToMainMenu()
        {
            GameFlow.LeavingScene = true;
            SceneManager.LoadScene(titleSceneName);
        }

        public void QuitGame()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
