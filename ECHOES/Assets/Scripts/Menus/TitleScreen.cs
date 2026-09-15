using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Echoes.Saving;

namespace Echoes.Menus
{
    // Vive en su propia escena (Title). No referencia nada de la escena de juego —no se puede,
    // Unity no permite arrastrar objetos entre escenas distintas—: solo marca la intención en
    // GameFlow y carga la escena de juego. Es GameEntryPoint, ya en esa escena, quien decide
    // qué hacer con esa intención.
    //
    // Estilo Pokémon NDS en dos pasos: primero una imagen fija con "Press Start" (SplashPanel),
    // y solo al pulsar algo se revela el menú de botones (MenuPanel).
    public class TitleScreen : MonoBehaviour
    {
        [SerializeField] private GameObject splashPanel;
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private Button continueButton;
        [Tooltip("Botón que queda seleccionado al aparecer el menú, para que las flechas/D-pad funcionen sin haber hecho clic antes. Normalmente New Game.")]
        [SerializeField] private GameObject firstSelectedButton;
        [Tooltip("Nombre exacto de la escena de juego (la que tiene TrainingCourt.unity), tal cual aparece en Build Settings.")]
        [SerializeField] private string gameSceneName = "TrainingCourt";
        [Tooltip("Panel negro a pantalla completa. Se reutiliza en los dos sentidos: funde A negro antes de cargar la partida, y esta propia escena nace ya en negro y se retira con un fundido de vuelta al cargar (p. ej. al volver desde Main Menu, que ya funde a negro antes de traer aquí).")]
        [SerializeField] private CanvasGroup fadeOverlay;
        [SerializeField] private float fadeToBlackTime = 0.5f;
        [SerializeField] private float revealFromBlackTime = 0.6f;

        private InputAction _submitAction;
        private InputAction _cancelAction;
        private bool _loading;

        private void Awake()
        {
            // Escena de Título recién cargada: cualquier "nos vamos" de la partida anterior ya
            // se ha cumplido.
            GameFlow.LeavingScene = false;
            // El Título es un menú de principio a fin — el cursor debe verse siempre, aunque se
            // llegue aquí justo después de una partida (donde se oculta durante el juego).
            Cursor.visible = true;

            _submitAction = InputSystem.actions.FindAction("UI/Submit");
            _cancelAction = InputSystem.actions.FindAction("UI/Cancel");
            if (splashPanel != null) splashPanel.SetActive(true);
            if (menuPanel != null) menuPanel.SetActive(false);
            // Nace en negro: si se llega aquí tras un fundido desde la partida (Main Menu), no
            // hay corte; si es el primer arranque de la app, es simplemente un fundido de
            // entrada. Se retira solo en Start() (ver RevealFromBlack).
            if (fadeOverlay != null)
            {
                fadeOverlay.alpha = 1f;
                fadeOverlay.blocksRaycasts = true;
            }
        }

        private void Start()
        {
            // Como en Pokémon: sin partida guardada, Continue directamente no existe (no solo
            // se ve gris) — con un Vertical Layout Group en MenuPanel, los demás botones se
            // reacomodan solos al quedar este desactivado.
            if (continueButton != null)
                continueButton.gameObject.SetActive(SaveSystem.HasSave());

            if (fadeOverlay != null) StartCoroutine(RevealFromBlack());
        }

        private IEnumerator RevealFromBlack()
        {
            float t = 0f;
            while (t < revealFromBlackTime)
            {
                t += Time.deltaTime;
                fadeOverlay.alpha = Mathf.Lerp(1f, 0f, t / revealFromBlackTime);
                yield return null;
            }
            fadeOverlay.alpha = 0f;
            // El CanvasGroup del overlay cubre TODA la pantalla (está por encima de MenuPanel):
            // bajar el alpha a 0 no desactiva por sí solo el bloqueo de raycasts, así que sin
            // esto se queda invisible pero tapando todo el ratón —clics y hover— del menú
            // durante el resto de la escena de Título, aunque el teclado/mando funcionen bien al
            // no depender de raycasts.
            fadeOverlay.blocksRaycasts = false;
        }

        private void Update()
        {
            if (splashPanel != null && splashPanel.activeSelf)
            {
                if (AnyInputPressed()) ShowMenu();
                return;
            }

            if (menuPanel != null && menuPanel.activeSelf)
            {
                // Un clic en cualquier hueco vacío del menú (fuera de los botones) deselecciona
                // el EventSystem por defecto de Unity — sin nada seleccionado, ni el teclado ni
                // el mando pueden navegar (las flechas mueven la selección A PARTIR de la actual,
                // que ya no existe), y antes solo se arreglaba saliendo al splash y reabriendo el
                // menú. Se restaura solo el mismo objetivo por defecto de ShowMenu(), cada frame
                // que haga falta.
                if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null)
                    SelectDefaultButton();

                // Desde el menú, "atrás" vuelve al splash — igual que un submenú se cierra hacia
                // el que lo abrió. No interfiere con Opciones: mientras esa está abierta,
                // menuPanel ya está desactivado (OptionsMenu.Show lo apaga como callerPanel), así
                // que este bloque ni se plantea correr.
                if (CancelPressed())
                    HideMenu();
            }
        }

        private bool AnyInputPressed()
        {
            if (_submitAction != null && _submitAction.WasPressedThisFrame()) return true;
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) return true;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
            return false;
        }

        private bool CancelPressed()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) return true;
            return _cancelAction != null && _cancelAction.WasPressedThisFrame();
        }

        private void ShowMenu()
        {
            splashPanel.SetActive(false);
            if (menuPanel != null) menuPanel.SetActive(true);

            // Sin esto, un mando/teclado no puede navegar el menú hasta hacer clic una vez.
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                SelectDefaultButton();
            }
        }

        // Prioriza Continue si hay partida guardada — más cómodo que tener que navegar hasta él
        // cada vez. Sin partida, cae en New Game (firstSelectedButton).
        private void SelectDefaultButton()
        {
            GameObject target = (continueButton != null && continueButton.gameObject.activeSelf)
                ? continueButton.gameObject
                : firstSelectedButton;
            if (target != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(target);
        }

        // Público para poder colgarlo también del OnClick de un botón "Atrás" en pantalla,
        // además del propio Cancel/Esc que ya lo llama desde Update().
        public void HideMenu()
        {
            menuPanel.SetActive(false);
            if (splashPanel != null) splashPanel.SetActive(true);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }

        public void OnNewGame()
        {
            if (_loading) return;
            // Vertical Slice con un único slot de guardado: "New Game" de verdad tiene que
            // empezar de cero, no dejar la partida de Continue a medias por debajo. Sin aviso de
            // confirmación ni slots múltiples a propósito — fuera del alcance de esta demo.
            SaveSystem.DeleteSave();
            GameFlow.ResetSession();
            GameFlow.ContinueRequested = false;
            StartTransition();
        }

        public void OnContinue()
        {
            if (_loading || !SaveSystem.HasSave()) return;
            GameFlow.ResetSession();
            GameFlow.ContinueRequested = true;
            StartTransition();
        }

        private void StartTransition()
        {
            _loading = true;
            StartCoroutine(FadeToGame());
        }

        // Funde A negro aquí, en Title; la escena de juego ya nace con la pantalla negra
        // (AutosaveNotice.Awake empieza a alpha 0, pero GameEntryPoint la pone a 1 antes de que
        // se dibuje el primer frame) y solo anima la entrada del texto — así el corte entre
        // escenas no se nota.
        private IEnumerator FadeToGame()
        {
            if (fadeOverlay != null)
            {
                fadeOverlay.blocksRaycasts = true;
                float t = 0f;
                while (t < fadeToBlackTime)
                {
                    t += Time.deltaTime;
                    fadeOverlay.alpha = Mathf.Lerp(0f, 1f, t / fadeToBlackTime);
                    yield return null;
                }
                fadeOverlay.alpha = 1f;
            }
            SceneManager.LoadScene(gameSceneName);
        }

        public void OnQuit()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
