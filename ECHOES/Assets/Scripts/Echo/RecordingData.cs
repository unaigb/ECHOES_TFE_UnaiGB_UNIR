using UnityEngine;
using System.Collections.Generic;
using Echoes.Interactables;

namespace Echoes.Echo
{
    public enum EchoState { Idle, Recording, Recorded, Playing }

    [System.Serializable]
    public class FrameSnapshot
    {
        public float time;
        public Vector2 position;
    }

    public class InteractionEvent
    {
        public float time;
        public IInteractable target;
    }

    public class RecordingData
    {
        public Vector2 startPosition;
        public List<FrameSnapshot> frames = new();
        public List<InteractionEvent> interactions = new();

        public void Clear()
        {
            frames.Clear();
            interactions.Clear();
        }
    }
}
