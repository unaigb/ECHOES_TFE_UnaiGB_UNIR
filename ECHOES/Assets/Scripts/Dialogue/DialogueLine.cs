using UnityEngine;

namespace Echoes.Dialogue
{
    // Registro de voz de Ir1s. De momento solo es informativo; más adelante puede usarse
    // para variar el color del texto, el retrato o un efecto de sonido según el registro
    // (protocolario vs. con fisura), tal y como se describe en el apartado de Narrativa.
    public enum DialogueRegister { Protocolario, ConFisura }

    [System.Serializable]
    public class DialogueLine
    {
        [TextArea(2, 5)]
        public string text;
        public DialogueRegister register = DialogueRegister.Protocolario;
    }
}
