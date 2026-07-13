using System;
using System.Globalization;
using UnityEngine;

namespace MiniGame.Core.Runtime.PlatformSDK
{
    /// <summary>
    /// 云存档冲突解决策略。
    /// </summary>
    public enum CloudSaveConflictPolicy
    {
        LocalWins,
        CloudWins,
        TimestampWins
    }

    /// <summary>
    /// 云存档管理器：本地 PlayerPrefs + 云端混合存储。
    /// </summary>
    public class CloudSaveManager
    {
        public static CloudSaveManager Instance { get; } = new CloudSaveManager();

        /// <summary>
        /// 底层平台 SDK。
        /// </summary>
        public IPlatformSDK SDK { get; set; }

        /// <summary>
        /// 冲突解决策略。
        /// </summary>
        public CloudSaveConflictPolicy ConflictPolicy = CloudSaveConflictPolicy.TimestampWins;

        /// <summary>
        /// 初始化云存档管理器。
        /// </summary>
        public void Initialize(IPlatformSDK sdk = null)
        {
            SDK = sdk ?? PlatformSDKFactory.Create();
        }

        /// <summary>
        /// 保存数据：先写本地，再同步到云端。
        /// </summary>
        public void Save(string key, string data, Action<bool> callback = null)
        {
            SetLocalData(key, data);

            if (SDK == null)
            {
                callback?.Invoke(true);
                return;
            }

            SDK.SaveToCloud(key, data, callback);
        }

        /// <summary>
        /// 加载数据：拉取云端，与本地合并后返回。
        /// </summary>
        public void Load(string key, Action<string> callback)
        {
            var local = GetLocalData(key);

            if (SDK == null)
            {
                callback?.Invoke(local);
                return;
            }

            SDK.LoadFromCloud(key, cloudData =>
            {
                var resolved = ResolveConflict(key, local, cloudData);
                callback?.Invoke(resolved);
            });
        }

        /// <summary>
        /// 删除本地与云端数据。
        /// </summary>
        public void Delete(string key, Action<bool> callback = null)
        {
            PlayerPrefs.DeleteKey(DataKey(key));
            PlayerPrefs.DeleteKey(TimestampKey(key));

            if (SDK == null)
            {
                callback?.Invoke(true);
                return;
            }

            SDK.SaveToCloud(key, string.Empty, callback);
        }

        private string ResolveConflict(string key, string local, string cloud)
        {
            if (string.IsNullOrEmpty(cloud)) return local;
            if (string.IsNullOrEmpty(local)) return cloud;

            switch (ConflictPolicy)
            {
                case CloudSaveConflictPolicy.LocalWins:
                    return local;
                case CloudSaveConflictPolicy.CloudWins:
                    return cloud;
                case CloudSaveConflictPolicy.TimestampWins:
                default:
                    var localTicks = GetLocalTimestamp(key);
                    // 云端没有时间戳信息时默认使用云端（通常云端更新）
                    if (localTicks <= 0) return cloud;
                    return localTicks >= DateTime.UtcNow.Ticks ? local : cloud;
            }
        }

        private void SetLocalData(string key, string data)
        {
            PlayerPrefs.SetString(DataKey(key), data);
            PlayerPrefs.SetString(TimestampKey(key), DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture));
            PlayerPrefs.Save();
        }

        private string GetLocalData(string key)
        {
            return PlayerPrefs.GetString(DataKey(key), string.Empty);
        }

        private long GetLocalTimestamp(string key)
        {
            var ts = PlayerPrefs.GetString(TimestampKey(key), string.Empty);
            if (long.TryParse(ts, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ticks))
            {
                return ticks;
            }
            return 0;
        }

        private static string DataKey(string key)
        {
            return $"MiniGame.CloudSave.{key}";
        }

        private static string TimestampKey(string key)
        {
            return $"MiniGame.CloudSave.{key}.ts";
        }
    }
}
