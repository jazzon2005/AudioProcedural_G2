using UnityEngine;

namespace View.Control
{
    public class GameBoardAudio : MonoBehaviour
    {
        private GameAudio _gameAudio;

        private void Awake()
        {
            EnsureAudioReference();
        }

        private void EnsureAudioReference()
        {
            if (_gameAudio == null)
            {
                _gameAudio = GameAudio.Instance;
                if (_gameAudio == null)
                {
                    var audioGo = GameObject.FindGameObjectWithTag("GameAudio");
                    if (audioGo != null)
                    {
                        _gameAudio = audioGo.GetComponent<GameAudio>();
                    }
                }
            }
        }

        public ProceduralAudioSynth Play(GameClip clip, float delay = 0f, float volume = 1f, float startTime = 0f)
        {
            if (!enabled) return null;
            EnsureAudioReference();
            return _gameAudio != null ? _gameAudio.Play(clip, delay, volume, startTime) : null;
        }

        public void Play(MusicClip clip, float fadeTime = 0f, float delay = 0f, float volume = 1f, float startTime = 0f)
        {
            if (!enabled) return;
            EnsureAudioReference();
            _gameAudio?.Play(clip, fadeTime, delay, volume, startTime);
        }
    }
}