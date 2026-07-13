using System.Collections.Generic;
using UnityEngine;

namespace MiniGame.StarterKit.Runtime.Audio
{
    /// <summary>
    /// 音频管理器。支持背景音乐、音效、音量控制、静音。
    /// </summary>
    public class MGSAudioManager : MonoBehaviour
    {
        private static MGSAudioManager _instance;

        public static MGSAudioManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("MGSAudioManager");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<MGSAudioManager>();
                    _instance.InitializeSources();
                }
                return _instance;
            }
        }

        [SerializeField]
        private AudioSource _musicSource;

        [SerializeField]
        private AudioSource _sfxSource;

        private readonly Queue<AudioSource> _sfxPool = new Queue<AudioSource>();
        private float _musicVolume = 1f;
        private float _sfxVolume = 1f;
        private bool _isMuted;

        private void InitializeSources()
        {
            if (_musicSource == null)
            {
                _musicSource = gameObject.AddComponent<AudioSource>();
                _musicSource.loop = true;
            }

            if (_sfxSource == null)
            {
                _sfxSource = gameObject.AddComponent<AudioSource>();
            }
        }

        /// <summary>
        /// 播放背景音乐。
        /// </summary>
        public void PlayMusic(AudioClip clip, bool loop = true)
        {
            if (_musicSource == null || clip == null) return;
            _musicSource.clip = clip;
            _musicSource.loop = loop;
            _musicSource.volume = _isMuted ? 0f : _musicVolume;
            _musicSource.Play();
        }

        /// <summary>
        /// 停止背景音乐。
        /// </summary>
        public void StopMusic()
        {
            if (_musicSource == null) return;
            _musicSource.Stop();
        }

        /// <summary>
        /// 播放音效。
        /// </summary>
        public void PlaySfx(AudioClip clip)
        {
            if (clip == null) return;

            AudioSource source = GetSfxSource();
            source.clip = clip;
            source.volume = _isMuted ? 0f : _sfxVolume;
            source.Play();
        }

        /// <summary>
        /// 设置背景音乐音量。
        /// </summary>
        public void SetMusicVolume(float volume)
        {
            _musicVolume = Mathf.Clamp01(volume);
            if (_musicSource != null)
            {
                _musicSource.volume = _isMuted ? 0f : _musicVolume;
            }
        }

        /// <summary>
        /// 设置音效音量。
        /// </summary>
        public void SetSfxVolume(float volume)
        {
            _sfxVolume = Mathf.Clamp01(volume);
        }

        /// <summary>
        /// 设置全局静音。
        /// </summary>
        public void SetMuted(bool muted)
        {
            _isMuted = muted;
            AudioListener.pause = muted;
            if (_musicSource != null)
            {
                _musicSource.volume = muted ? 0f : _musicVolume;
            }
        }

        private AudioSource GetSfxSource()
        {
            foreach (var source in _sfxPool)
            {
                if (source != null && !source.isPlaying)
                {
                    return source;
                }
            }

            var newSource = gameObject.AddComponent<AudioSource>();
            _sfxPool.Enqueue(newSource);
            return newSource;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
