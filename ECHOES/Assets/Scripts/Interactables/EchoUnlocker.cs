using UnityEngine;
using Echoes.Echo;
using Echoes.Player;
using Echoes.Dialogue;

namespace Echoes.Interactables
{
    public class EchoUnlocker : MonoBehaviour, IInteractable
    {
        [SerializeField] private EchoRecorder echoRecorder;
        [SerializeField] private PlayerAnimator playerAnimator;
        [Tooltip("Opcional: diálogo de Ir1s que se lanza al desbloquear el eco (registro protocolario de la Sala 02).")]
        [SerializeField] private DialogueSequence unlockDialogue;

        private SpriteRenderer _renderer;
        private bool _used = false;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        public void Interact()
        {
            if (_used) return;
            _used = true;

            echoRecorder.Unlock();
            playerAnimator.SetEcoUnlocked(true);

            if (_renderer != null)
                _renderer.color = new Color(0.35f, 0.35f, 0.35f, 1f);

            if (unlockDialogue != null && DialogueManager.Instance != null)
                DialogueManager.Instance.Play(unlockDialogue);
        }
    }
}
