using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniGame.StarterKit.Runtime.Audio
{
    /// <summary>
    /// 音频配置表。用于通过 ID 播放背景音乐与音效，便于策划批量管理。
    /// </summary>
    [CreateAssetMenu(fileName = "MGSAudioConfig", menuName = "MiniGame/Audio Config")]
    public class MGSAudioConfig : ScriptableObject
    {
        [Serializable]
        public class AudioEntry
        {
            [Tooltip("唯一标识，代码中通过该 ID 播放")]
            public string Id;

            [Tooltip("音频资源")]
            public AudioClip Clip;

            [Tooltip("是否为背景音乐，同一时间只播放一首 BGM")]
            public bool IsMusic;

            [Tooltip("默认音量")]
            [Range(0f, 1f)]
            public float Volume = 1f;

            [Tooltip("默认音高")]
            [Range(0.5f, 2f)]
            public float Pitch = 1f;

            [Tooltip("是否循环（仅对 BGM 生效）")]
            public bool Loop;
        }

        [Tooltip("音频条目列表")]
        public List<AudioEntry> Entries = new List<AudioEntry>();

        /// <summary>
        /// 根据 ID 查找音频条目。
        /// </summary>
        public AudioEntry Find(string id)
        {
            if (string.IsNullOrEmpty(id) || Entries == null) return null;
            foreach (var entry in Entries)
            {
                if (entry != null && entry.Id == id)
                    return entry;
            }
            return null;
        }
    }
}
