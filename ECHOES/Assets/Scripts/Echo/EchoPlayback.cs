using UnityEngine;

namespace Echoes.Echo
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class EchoPlayback : MonoBehaviour
    {
        private RecordingData _data;
        private int _frameIndex;
        private int _interactionIndex;
        private float _playbackTimer;
        private bool _playing;
        private EchoRecorder _recorder;
        private Rigidbody2D _rb;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.gravityScale = 0f;
        }

        public void Play(RecordingData data)
        {
            _data = data;
            _frameIndex = 0;
            _interactionIndex = 0;
            _playbackTimer = 0f;
            _playing = true;
            _recorder = FindFirstObjectByType<EchoRecorder>();

            Collider2D playerCollider = GameObject.FindWithTag("Player")?.GetComponent<Collider2D>();
            Collider2D echoCollider = GetComponent<Collider2D>();
            if (playerCollider != null && echoCollider != null)
                Physics2D.IgnoreCollision(echoCollider, playerCollider, true);
        }

        private void FixedUpdate()
        {
            if (!_playing || _data == null) return;

            _playbackTimer += Time.fixedDeltaTime;

            while (_frameIndex < _data.frames.Count && _data.frames[_frameIndex].time <= _playbackTimer)
            {
                _rb.MovePosition(_data.frames[_frameIndex].position);
                _frameIndex++;
            }

            while (_interactionIndex < _data.interactions.Count &&
                   _data.interactions[_interactionIndex].time <= _playbackTimer)
            {
                _data.interactions[_interactionIndex].target?.Interact();
                _interactionIndex++;
            }

            if (_frameIndex >= _data.frames.Count)
            {
                _playing = false;
                _recorder?.OnEchoFinished();
                Destroy(gameObject, 0.1f);
            }
        }
    }
}
