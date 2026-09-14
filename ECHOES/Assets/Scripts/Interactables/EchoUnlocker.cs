using UnityEngine;
using Echoes.Echo;
using Echoes.Player;
using Echoes.Dialogue;
using Echoes.Audio;

namespace Echoes.Interactables
{
    public class EchoUnlocker : MonoBehaviour, IInteractable
    {
        [SerializeField] private EchoRecorder echoRecorder;
        [SerializeField] private PlayerAnimator playerAnimator;
        [Tooltip("Opcional: diálogo de Ir1s que se lanza al desbloquear el eco (registro protocolario de la Sala 02).")]
        [SerializeField] private DialogueSequence unlockDialogue;
        [SerializeField] private AudioClip unlockSfx;
        [Range(0f, 5f)] [SerializeField] private float unlockSfxVolume = 1f;

        private SpriteRenderer _renderer;
        private bool _used = false;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        private void OnEnable()
        {
            if (echoRecorder != null) echoRecorder.OnUnlocked += HandleUnlocked;
        }

        private void OnDisable()
        {
            if (echoRecorder != null) echoRecorder.OnUnlocked -= HandleUnlocked;
        }

        public void Interact()
        {
            if (_used) return;

            echoRecorder.Unlock();
            playerAnimator.SetEcoUnlocked(true);
            AudioManager.Instance?.PlaySfx(unlockSfx, unlockSfxVolume);

            if (unlockDialogue != null && DialogueManager.Instance != null)
                DialogueManager.Instance.Play(unlockDialogue);
        }

        // Se dispara tanto si desbloquea Interact() como si lo hace SaveManager.ApplyLoadedSave()
        // al continuar una partida (la escena se recarga entera, así que sin esto el botón
        // volvería a aparecer "sin usar" aunque el eco ya estuviera desbloqueado de antes).
        private void HandleUnlocked()
        {
            if (_used) return;
            _used = true;
            if (_renderer != null)
                _renderer.color = new Color(0.35f, 0.35f, 0.35f, 1f);
        }
    }
}
