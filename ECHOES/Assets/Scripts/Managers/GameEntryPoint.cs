using UnityEngine;
using Echoes.Player;
using Echoes.Camera;
using Echoes.UI;
using Echoes.Interactables;
using Echoes.Saving;
using Echoes.Audio;

namespace Echoes.Managers
{
    // Decide cómo arranca la escena de juego: si se viene de "Continue" en el título (o de un
    // salto de sala de depuración), se salta la cinemática de entrada; si no, se reproduce
    // normalmente. Sustituye al antiguo auto-arranque de IntroCutscene.
    //
    // Todo lo que "coloca" el estado inicial (snap de cámara al continuar, o encuadre de cámara
    // de la cinemática) se hace ANTES del aviso de autoguardado, mientras la pantalla está en
    // negro — así, al retirarse el aviso, no se ve ningún ajuste: en Continue, el mundo ya está
    // en su sitio; en partida nueva, la cinemática arranca como si se le diera a "play".
    public class GameEntryPoint : MonoBehaviour
    {
        [SerializeField] private IntroCutscene introCutscene;
        [SerializeField] private SaveManager saveManager;
        [SerializeField] private PlayerInputHandler playerInput;
        [SerializeField] private Rigidbody2D playerRigidbody;
        [SerializeField] private CameraFollow cameraFollow;
        [Tooltip("CanvasGroup del HUD de juego — el mismo que usa IntroCutscene.")]
        [SerializeField] private CanvasGroup hudCanvasGroup;
        [Tooltip("El mismo indicador de sala (\"LocationBox\") que usa PauseMenu — no cuelga del CanvasGroup del HUD, así que hay que ocultarlo/mostrarlo aparte.")]
        [SerializeField] private GameObject locationBox;
        [Tooltip("Las mismas barras de letterbox que usa IntroCutscene.")]
        [SerializeField] private LetterboxBars letterbox;
        [Tooltip("La misma puerta de entrada que cierra IntroCutscene al terminar el paseo.")]
        [SerializeField] private Door startDoor;
        [Tooltip("Opcional: aviso de autoguardado (pantalla de carga). Se reproduce una sola vez por sesión, antes de revelar la partida. Déjalo vacío para saltárselo.")]
        [SerializeField] private AutosaveNotice autosaveNotice;

        private bool _isContinue;
        private bool _isDebugJump;
        private bool _willPlayIntro;

        private void Start()
        {
            // Escena nueva de verdad empezando — cualquier "nos vamos" de la anterior ya se
            // cumplió, así que a partir de aquí el audio vuelve a sonar con normalidad.
            GameFlow.LeavingScene = false;
            // Oculto durante la partida — PauseMenu lo vuelve a mostrar mientras está abierto.
            Cursor.visible = false;

            _isDebugJump = LevelManager.Instance != null && LevelManager.Instance.IsDebugJump;
            _isContinue = GameFlow.ContinueRequested;
            GameFlow.ContinueRequested = false;
            _willPlayIntro = !_isContinue && !_isDebugJump && introCutscene != null;

            // Oculto desde ya: mientras dura el aviso de autoguardado la pantalla está en negro,
            // y sin esto el HUD —sobre todo LocationBox, que no cuelga del CanvasGroup— se vería
            // flotando encima.
            if (hudCanvasGroup != null) hudCanvasGroup.alpha = 0f;
            if (locationBox != null) locationBox.SetActive(false);

            if (_willPlayIntro)
                introCutscene.PrepareForReveal();
            else
                PrepareContinueState();

            // El fundido de revelado pasa SIEMPRE que entramos de verdad a la partida (Continue,
            // detección, Restart Room, partida nueva) — si no, tras una recarga por detección el
            // jugador simplemente reaparece en su sitio sin más. El texto del mensaje, en cambio,
            // solo la primera vez de la sesión. El salto de sala de depuración es la única
            // excepción a todo esto: es solo para probar en el Editor, sin perder tiempo en cada
            // reinicio del modo Play.
            bool showFade = !_isDebugJump && autosaveNotice != null;
            if (showFade)
            {
                bool showText = !GameFlow.AutosaveNoticeShown;
                GameFlow.AutosaveNoticeShown = true;
                autosaveNotice.Play(showText, Reveal);
            }
            else
            {
                // El salto de depuración se salta el fundido de ESTA vez, pero el aviso de
                // "una vez por sesión" hay que darlo igualmente por mostrado — si no, el primer
                // Restart Room/detección tras probar así encontraba el aviso sin marcar y
                // enseñaba el mensaje de golpe, cuando lo único que tocaba saltarse era el
                // fundido del debug jump en sí.
                if (_isDebugJump) GameFlow.AutosaveNoticeShown = true;
                Reveal();
            }
        }

        // Todo lo que antes hacía el salto de Continue/debug jump, salvo lo que se VE (HUD,
        // LocationBox, input) — eso se deja para Reveal(), que se llama cuando la pantalla negra
        // ya se ha retirado. Así, mientras dura el aviso, el mundo ya se coloca en su sitio sin
        // que se vea ningún ajuste al descubrirlo.
        private void PrepareContinueState()
        {
            // Sin esto, InputEnabled se queda en su valor por defecto (true) durante todo el
            // fundido de revelado — se podía andar y hasta grabar/desplegar/rebobinar un eco con
            // la pantalla todavía en negro. Reveal() ya lo vuelve a activar cuando toca.
            if (playerInput != null) playerInput.InputEnabled = false;

            GameFlow.StartTimerIfNeeded();
            if (_isContinue && saveManager != null) saveManager.ApplyLoadedSave();

            // Sin cinemática de por medio, nadie retira las barras ni corrige la cámara:
            // Awake() de LetterboxBars siempre las despliega, y CameraFollow arrancaría con
            // un barrido/zoom visible desde su posición por defecto hasta la real.
            if (letterbox != null) letterbox.HideImmediate();
            if (cameraFollow != null && playerRigidbody != null)
                cameraFollow.SnapToCurrentState(playerRigidbody.position);

            // Sin cinemática, nadie llama a startDoor.Close() — se queda con el estado por
            // defecto de la escena (abierta), dejando salir al vacío detrás del punto de inicio.
            // SetInitialState(), no Close(): esto es una restauración silenciosa a un estado ya
            // sabido (la puerta lleva cerrada desde la cinemática original), no un cierre en vivo
            // delante del jugador — con Close() sonaba en cada Continue/Restart Room, incluso a
            // varias salas de distancia (se oía igual en Hall 01-A/B por el sonido posicional).
            if (startDoor != null) startDoor.SetInitialState(false);
        }

        private void Reveal()
        {
            // A partir de aquí el mundo ya es visible — seguro para que un diálogo en cola
            // (p. ej. uno de OnRoomEntered disparado durante PrepareContinueState, mientras el
            // aviso de autoguardado seguía tapando la pantalla) arranque de verdad.
            GameFlow.SilentSetup = false;
            AudioManager.Instance?.BeginRoomMusic();

            if (_isContinue || _isDebugJump)
            {
                if (hudCanvasGroup != null) hudCanvasGroup.alpha = 1f;
                if (locationBox != null) locationBox.SetActive(true);
                if (playerInput != null) playerInput.InputEnabled = true;
            }
            else if (_willPlayIntro)
            {
                introCutscene.Begin();
            }
        }
    }
}
