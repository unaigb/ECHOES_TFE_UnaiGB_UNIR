using UnityEngine;
using Echoes.Echo;
using Echoes.Player;

namespace Echoes.Interactables
{
    public class EchoUnlocker : MonoBehaviour, IInteractable
    {
        [SerializeField] private EchoRecorder echoRecorder;
        [SerializeField] private PlayerAnimator playerAnimator;

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
        }
    }
}
