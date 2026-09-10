using System.Collections;
using UnityEngine;
using Echoes.Managers;

namespace Echoes.Dialogue
{
    // Dispara una DialogueSequence en un momento concreto: al empezar la escena, al pisar un
    // trigger físico, o al entrar en una sala concreta (vía LevelManager.OnRoomChanged).
    // También se puede llamar a Fire() desde otro script o desde un UnityEvent del Inspector.
    public class DialogueTrigger : MonoBehaviour
    {
        public enum Mode { Manual, OnStart, OnTriggerEnter, OnRoomEntered }

        [SerializeField] private DialogueSequence sequence;
        [SerializeField] private Mode mode = Mode.Manual;
        [Tooltip("Solo para OnRoomEntered: nombre exacto de la sala, el mismo texto que en RoomBoundary.RoomName.")]
        [SerializeField] private string roomName;
        [Tooltip("Espera antes de lanzar el diálogo (segundos, tiempo real).")]
        [SerializeField] private float delay = 0.2f;
        [SerializeField] private bool playOnce = true;

        private bool _fired;

        private void Awake()
        {
            // Comodidad: si es un trigger espacial y tiene collider, lo pone en modo trigger solo.
            if (mode == Mode.OnTriggerEnter && TryGetComponent(out Collider2D col))
                col.isTrigger = true;
        }

        private void Start()
        {
            if (mode == Mode.OnRoomEntered && LevelManager.Instance != null)
                LevelManager.Instance.OnRoomChanged += HandleRoomChanged;

            if (mode == Mode.OnStart) Fire();
        }

        private void OnDisable()
        {
            if (mode == Mode.OnRoomEntered && LevelManager.Instance != null)
                LevelManager.Instance.OnRoomChanged -= HandleRoomChanged;
        }

        private void HandleRoomChanged(string entered)
        {
            if (entered == roomName) Fire();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (mode != Mode.OnTriggerEnter) return;
            if (!other.CompareTag("PlayerFeet")) return;
            Fire();
        }

        public void Fire()
        {
            if (playOnce && _fired) return;
            if (sequence == null || DialogueManager.Instance == null) return;
            _fired = true;

            if (delay > 0f) StartCoroutine(FireDelayed());
            else DialogueManager.Instance.Play(sequence);
        }

        private IEnumerator FireDelayed()
        {
            yield return new WaitForSecondsRealtime(delay);
            if (DialogueManager.Instance != null) DialogueManager.Instance.Play(sequence);
        }
    }
}
