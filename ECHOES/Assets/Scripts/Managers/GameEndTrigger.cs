using UnityEngine;
using Echoes.Dialogue;
using Echoes.Saving;

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
                DialogueManager.Instance.Play(endingDialogue, StartEnding);
            else
                StartEnding();
        }

        // La puerta final suele cerrarse sola justo por aquí (AutoCloseDoor, con un pequeño
        // delay tras cruzarla) — sin esto, su sonido de cierre podía colarse en pleno fundido a
        // negro o encima de la lámina de cómic. GameFlow.LeavingScene ya lo consulta
        // AudioManager.PlaySfx/PlaySfxAt para no sonar en momentos así; se pone justo aquí (no
        // antes) para no silenciar de paso el sonido de avanzar del diálogo de cierre.
        private void StartEnding()
        {
            GameFlow.LeavingScene = true;
            endingSequence.Show();
        }
    }
}
