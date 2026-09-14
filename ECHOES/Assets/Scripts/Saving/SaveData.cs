namespace Echoes.Saving
{
    // Guardado mínimo: sala actual + si el eco está desbloqueado. No persiste el estado de
    // puzles individuales (puertas, placas, cajas) — "Continue" reaparece al principio de la
    // última sala alcanzada, como si esa sala se reiniciara.
    [System.Serializable]
    public class SaveData
    {
        public string roomName;
        public bool echoUnlocked;
    }
}
