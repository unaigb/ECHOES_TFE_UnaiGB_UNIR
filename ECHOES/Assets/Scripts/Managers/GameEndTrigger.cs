using UnityEngine;
using Echoes.Dialogue;

namespace Echoes.Managers
{
    [RequireComponent(typeof(Collider2D))]
    public class GameEndTrigger : MonoBehaviour
    {
        [SerializeField] private EndingSequence endingSequence;
        [Tooltip("Opcional: segunda intervención de cierre de Ir1s (registro con fisura). Se reproduce antes de la secuencia de final.")]
        [SerializeField] private DialogueSequence endingDialogue;

        private bool _triggered;

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_triggered || !other.CompareTag("PlayerFeet")) return;
            _triggered = true;

            if (endingDialogue != null && DialogueManager.Instance != null)
                DialogueManager.Instance.Play(endingDialogue, () => endingSequence.Show());
            else
                endingSequence.Show();
        }
    }
}
