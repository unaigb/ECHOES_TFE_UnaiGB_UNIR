using UnityEngine;

namespace Echoes.Echo
{
    public class EchoPlayback : MonoBehaviour
    {
        private RecordingData _data;
        private int _frameIndex;
        private float _playbackTimer;
        private bool _playing;
        private EchoRecorder _recorder;

        public void Play(RecordingData data)
        {
            _data = data;
            _frameIndex = 0;
            _playbackTimer = 0f;
            _playing = true;
            _recorder = FindFirstObjectByType<EchoRecorder>();
        }

        private void FixedUpdate()
        {
            if (!_playing || _data == null) return;

            _playbackTimer += Time.fixedDeltaTime;

            while (_frameIndex < _data.frames.Count && _data.frames[_frameIndex].time <= _playbackTimer)
            {
                transform.position = _data.frames[_frameIndex].position;
                _frameIndex++;
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
