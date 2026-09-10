using System;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using Echoes.Player;

namespace Echoes.Dialogue
{
    // Cuadro de diálogo de Ir1s. Lineal, sin ramificación: recibe una DialogueSequence y la
    // reproduce línea a línea con efecto de máquina de escribir, avanzando con la tecla de
    // interactuar (E), Espacio, Intro o clic. Mientras está activo, bloquea el input del
    // jugador y, opcionalmente, congela el tiempo de juego para poder leer sin ser detectado.
    //
    // Montaje: este componente va en un GameObject SIEMPRE ACTIVO (p. ej. el propio Canvas o
    // un objeto "DialogueSystem"). El cuadro visual se referencia aparte en 'panel' y se
    // muestra/oculta por alpha del CanvasGroup, no desactivando su GameObject.
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        [Header("Referencias de UI")]
        [SerializeField] private CanvasGroup panel;
        [SerializeField] private TextMeshProUGUI speakerLabel;
        [SerializeField] private TextMeshProUGUI bodyText;
        [Tooltip("Opcional: indicador de 'pulsa para continuar'. Se oculta mientras se escribe la línea.")]
        [SerializeField] private GameObject advanceIndicator;

        [Header("Jugador")]
        [SerializeField] private PlayerInputHandler playerInput;

        [Header("Ajustes")]
        [SerializeField] private float charsPerSecond = 45f;
        [Tooltip("Congela el juego (Time.timeScale = 0) mientras se muestra el diálogo. Recomendable para no ser detectado mientras se lee.")]
        [SerializeField] private bool pauseGameplay = true;
        [SerializeField] private float fadeTime = 0.15f;

        public bool IsActive { get; private set; }

        private InputAction _interactAction;

        private DialogueSequence _sequence;
        private Action _onComplete;
        private int _index;
        private bool _typing;
        private string _currentFull;
        private bool _prevInputEnabled;
        private Coroutine _typeRoutine;
        private Coroutine _fadeRoutine;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            _interactAction = InputSystem.actions.FindAction("Player/Interact");
            HideImmediate();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // Punto de entrada único. Si ya hay un diálogo en curso, lo corta y encadena el nuevo.
        public void Play(DialogueSequence sequence, Action onComplete = null)
        {
            if (sequence == null || sequence.lines == null || sequence.lines.Length == 0)
            {
                onComplete?.Invoke();
                return;
            }

            if (IsActive) EndImmediate(invokeCallback: false);

            _sequence = sequence;
            _onComplete = onComplete;
            _index = 0;
            IsActive = true;

            _prevInputEnabled = playerInput != null && playerInput.InputEnabled;
            if (playerInput != null) playerInput.InputEnabled = false;
            if (pauseGameplay) Time.timeScale = 0f;

            if (speakerLabel != null) speakerLabel.text = sequence.speakerName;
            panel.blocksRaycasts = true;
            StartFade(panel.alpha, 1f);

            ShowLine(0);
        }

        private void Update()
        {
            if (!IsActive) return;
            if (AdvancePressed()) OnAdvance();
        }

        private bool AdvancePressed()
        {
            if (_interactAction != null && _interactAction.WasPressedThisFrame()) return true;
            Keyboard kb = Keyboard.current;
            if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)) return true;
            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame) return true;
            return false;
        }

        private void OnAdvance()
        {
            if (_typing)
            {
                // Primera pulsación mientras escribe: completa la línea de golpe.
                if (_typeRoutine != null) StopCoroutine(_typeRoutine);
                bodyText.text = _currentFull;
                _typing = false;
                if (advanceIndicator != null) advanceIndicator.SetActive(true);
                return;
            }

            _index++;
            if (_index >= _sequence.lines.Length) { EndImmediate(invokeCallback: true); return; }
            ShowLine(_index);
        }

        private void ShowLine(int i)
        {
            _currentFull = ResolveTokens(_sequence.lines[i].text);
            if (advanceIndicator != null) advanceIndicator.SetActive(false);
            _typeRoutine = StartCoroutine(TypeLine(_currentFull));
        }

        // Sustituye tokens tipo {Move}, {Interact}, {Record} por la tecla/botón real según el
        // dispositivo en uso. Si no lleva "/", se asume el mapa "Player/".
        private static readonly Regex TokenRx = new Regex(@"\{([A-Za-z0-9_/]+)\}", RegexOptions.Compiled);

        private static string ResolveTokens(string raw)
        {
            if (string.IsNullOrEmpty(raw) || raw.IndexOf('{') < 0) return raw;
            return TokenRx.Replace(raw, m =>
            {
                string name = m.Groups[1].Value;
                if (!name.Contains("/")) name = "Player/" + name;
                return InputHints.Key(name);
            });
        }

        private IEnumerator TypeLine(string full)
        {
            _typing = true;
            bodyText.text = "";
            float delay = charsPerSecond > 0f ? 1f / charsPerSecond : 0f;
            for (int i = 0; i < full.Length; i++)
            {
                bodyText.text += full[i];
                if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
            }
            _typing = false;
            if (advanceIndicator != null) advanceIndicator.SetActive(true);
        }

        private void EndImmediate(bool invokeCallback)
        {
            if (_typeRoutine != null) StopCoroutine(_typeRoutine);
            _typing = false;
            IsActive = false;

            if (pauseGameplay) Time.timeScale = 1f;
            if (playerInput != null) playerInput.InputEnabled = _prevInputEnabled;

            panel.blocksRaycasts = false;
            StartFade(panel.alpha, 0f);

            Action cb = _onComplete;
            _onComplete = null;
            if (invokeCallback) cb?.Invoke();
        }

        private void StartFade(float from, float to)
        {
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(Fade(from, to));
        }

        private IEnumerator Fade(float from, float to)
        {
            float t = 0f;
            while (t < fadeTime)
            {
                t += Time.unscaledDeltaTime;
                panel.alpha = Mathf.Lerp(from, to, fadeTime > 0f ? t / fadeTime : 1f);
                yield return null;
            }
            panel.alpha = to;
            _fadeRoutine = null;
        }

        private void HideImmediate()
        {
            IsActive = false;
            if (panel != null)
            {
                panel.alpha = 0f;
                panel.blocksRaycasts = false;
            }
        }
    }
}
