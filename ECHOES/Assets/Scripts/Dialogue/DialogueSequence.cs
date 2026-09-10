using UnityEngine;

namespace Echoes.Dialogue
{
    // Una conversación completa de Ir1s en un momento concreto del tutorial.
    // Lineal, sin ramificación: se reproducen las líneas en orden.
    // Crear con: clic derecho en el Project → Create → ECHOES → Dialogue Sequence.
    [CreateAssetMenu(fileName = "Dialogue_", menuName = "ECHOES/Dialogue Sequence")]
    public class DialogueSequence : ScriptableObject
    {
        [Tooltip("Nombre que se muestra como emisor. Por defecto, Ir1s.")]
        public string speakerName = "Ir1s";

        public DialogueLine[] lines;
    }
}
