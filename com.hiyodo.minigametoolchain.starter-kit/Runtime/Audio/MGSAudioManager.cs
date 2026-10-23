using System.Collections.Generic;
using UnityEngine;

namespace MiniGame.StarterKit.Runtime.Audio
{
    /// <summary>
    /// 音频管理器。
    /// 支持背景音乐、音效、音量控制、静音、ID 配置表播放。
    /// 建议在场景中显式挂载一个 GameObject，或首次调用时自动创建。
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

        [Tooltip("音频配置表，可选。配置后可通过 ID 播放音频")]
        public MGSAudioConfig Config;

        [SerializeField]
        private AudioSource _musicSource;

        [SerializeField]
        private AudioSource _sfxSource;

        private readonly Queue<AudioSource> _sfxPool = new Queue<AudioSource>();
        private float _musicVolume = 1f;
        private float _sfxVolume = 1f;
        private bool _isMuted;

        private const string MusicVolumeKey = "MGS_MusicVolume";
        private const string SfxVolumeKey = "MGS_SfxVolume";
        private const string MutedKey = "MGS_Muted";

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSources();
            LoadPreferences();
        }

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

        private void LoadPreferences()
        {
            _musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
            _sfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
            _isMuted = PlayerPrefs.GetInt(MutedKey, 0) == 1;
            ApplyVolumes();
        }

        private void SavePreferences()
        {
            PlayerPrefs.SetFloat(MusicVolumeKey, _musicVolume);
            PlayerPrefs.SetFloat(SfxVolumeKey, _sfxVolume);
            PlayerPrefs.SetInt(MutedKey, _isMuted ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void ApplyVolumes()
        {
            if (_musicSource != null)
                _musicSource.volume = _isMuted ? 0f : _musicVolume;
            AudioListener.pause = _isMuted;
        }

        /// <summary>
        /// 播放背景音乐（AudioClip）。
        /// </summary>
        public void PlayMusic(AudioClip clip, bool loop = true, float volume = 1f)
        {
            if (_musicSource == null || clip == null) return;
            _musicSource.clip = clip;
            _musicSource.loop = loop;
            _musicSource.volume = _isMuted ? 0f : _musicVolume * Mathf.Clamp01(volume);
            _musicSource.pitch = 1f;
            _musicSource.Play();
        }

        /// <summary>
        /// 通过配置表 ID 播放背景音乐。
        /// </summary>
        public void PlayMusic(string id)
        {
            var entry = Config?.Find(id);
            if (entry == null || entry.Clip == null) return;
            PlayMusic(entry.Clip, entry.Loop, entry.Volume);
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
        /// 暂停背景音乐。
        /// </summary>
        public void PauseMusic()
        {
            if (_musicSource == null) return;
            _musicSource.Pause();
        }

        /// <summary>
        /// 恢复背景音乐。
        /// </summary>
        public void ResumeMusic()
        {
            if (_musicSource == null) return;
            _musicSource.UnPause();
        }

        /// <summary>
        /// 播放音效（AudioClip）。
        /// </summary>
        public void PlaySfx(AudioClip clip, float volumeScale = 1f, float pitch = 1f)
        {
            if (clip == null) return;

            var source = GetSfxSource();
            source.clip = clip;
            source.volume = _isMuted ? 0f : _sfxVolume * Mathf.Clamp01(volumeScale);
            source.pitch = pitch;
            source.loop = false;
            source.Play();
        }

        /// <summary>
        /// 通过配置表 ID 播放音效。
        /// </summary>
        public void PlaySfx(string id)
        {
            var entry = Config?.Find(id);
            if (entry == null || entry.Clip == null) return;
            PlaySfx(entry.Clip, entry.Volume, entry.Pitch);
        }

        /// <summary>
        /// 使用 OneShot 方式播放音效，适合短促重复音效。
        /// </summary>
        public void PlayOneShot(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null) return;
            var source = GetSfxSource();
            source.PlayOneShot(clip, _isMuted ? 0f : _sfxVolume * Mathf.Clamp01(volumeScale));
        }

        /// <summary>
        /// 设置背景音乐音量（0~1），并保存到 PlayerPrefs。
        /// </summary>
        public void SetMusicVolume(float volume)
        {
            _musicVolume = Mathf.Clamp01(volume);
            SavePreferences();
            ApplyVolumes();
        }

        /// <summary>
        /// 设置音效音量（0~1），并保存到 PlayerPrefs。
        /// </summary>
        public void SetSfxVolume(float volume)
        {
            _sfxVolume = Mathf.Clamp01(volume);
            SavePreferences();
        }

        /// <summary>
        /// 设置全局静音，并保存到 PlayerPrefs。
        /// </summary>
        public void SetMuted(bool muted)
        {
            _isMuted = muted;
            SavePreferences();
            ApplyVolumes();
        }

        /// <summary>
        /// 当前背景音乐音量。
        /// </summary>
        public float MusicVolume => _musicVolume;

        /// <summary>
        /// 当前音效音量。
        /// </summary>
        public float SfxVolume => _sfxVolume;

        /// <summary>
        /// 当前是否静音。
        /// </summary>
        public bool IsMuted => _isMuted;

        /// <summary>
        /// 静态便利方法：通过 ID 播放背景音乐。
        /// </summary>
        public static void PlayMusicById(string id) => Instance.PlayMusic(id);

        /// <summary>
        /// 静态便利方法：通过 ID 播放音效。
        /// </summary>
        public static void PlaySfxById(string id) => Instance.PlaySfx(id);

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
