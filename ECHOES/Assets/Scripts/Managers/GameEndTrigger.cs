using UnityEngine;

namespace Echoes.Managers
{
    [RequireComponent(typeof(Collider2D))]
    public class GameEndTrigger : MonoBehaviour
    {
        [SerializeField] private EndingSequence endingSequence;

        private bool _triggered;

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_triggered || !other.CompareTag("PlayerFeet")) return;
            _triggered = true;
            endingSequence.Show();
        }
    }
}
