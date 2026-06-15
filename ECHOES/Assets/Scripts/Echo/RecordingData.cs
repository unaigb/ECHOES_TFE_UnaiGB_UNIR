using UnityEngine;
using System.Collections.Generic;

namespace Echoes.Echo
{
    public enum EchoState { Idle, Recording, Recorded, Playing }

    [System.Serializable]
    public class FrameSnapshot
    {
        public float time;
        public Vector2 position;
    }

    public class RecordingData
    {
        public Vector2 startPosition;
        public List<FrameSnapshot> frames = new();

        public void Clear()
        {
            frames.Clear();
        }
    }
}
