using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MiniGame.Core.Editor.SettingsManager
{
    /// <summary>
    /// 记录 PlayerSettings、GraphicsSettings、QualitySettings、AudioManager 的关键字段，
    /// 用于一键优化前的备份与回滚。
    /// </summary>
    [Serializable]
    public class SettingsSnapshot
    {
        public string SnapshotId;
        public string CreatedAt;
        public string Comment;

        public Dictionary<string, object> PlayerSettings = new Dictionary<string, object>();
        public Dictionary<string, object> WebGLSettings = new Dictionary<string, object>();
        public Dictionary<string, object> QualitySettings = new Dictionary<string, object>();
        public Dictionary<string, object> AudioSettings = new Dictionary<string, object>();

        private static readonly string BackupDirectory = "Library/MiniGameToolchain/SettingsSnapshots";

        /// <summary>
        /// 创建当前项目设置的快照。
        /// </summary>
        public static SettingsSnapshot Capture(string comment = null)
        {
            var snapshot = new SettingsSnapshot
            {
                SnapshotId = Guid.NewGuid().ToString("N"),
                CreatedAt = DateTime.Now.ToString("O"),
                Comment = comment ?? "Auto-generated before optimization"
            };

            CapturePlayerSettings(snapshot.PlayerSettings);
            CaptureWebGLSettings(snapshot.WebGLSettings);
            CaptureQualitySettings(snapshot.QualitySettings);
            CaptureAudioSettings(snapshot.AudioSettings);

            return snapshot;
        }

        /// <summary>
        /// 将快照序列化保存到 Library 目录。
        /// </summary>
        public void Save()
        {
            Directory.CreateDirectory(BackupDirectory);
            var path = GetSnapshotPath(SnapshotId);
            var json = JsonUtility.ToJson(this, true);
            File.WriteAllText(path, json);
        }

        /// <summary>
        /// 从 Library 目录加载指定 ID 的快照。
        /// </summary>
        public static SettingsSnapshot Load(string snapshotId)
        {
            var path = GetSnapshotPath(snapshotId);
            if (!File.Exists(path)) return null;

            var json = File.ReadAllText(path);
            return JsonUtility.FromJson<SettingsSnapshot>(json);
        }

        /// <summary>
        /// 列出所有可用的快照 ID。
        /// </summary>
        public static List<string> ListSnapshotIds()
        {
            if (!Directory.Exists(BackupDirectory)) return new List<string>();

            return Directory.GetFiles(BackupDirectory, "snapshot_*.json")
                .Select(Path.GetFileNameWithoutExtension)
                .OrderByDescending(f => f)
                .ToList();
        }

        /// <summary>
        /// 应用快照中的设置（回滚）。
        /// </summary>
        public void Restore()
        {
            ApplyPlayerSettings(PlayerSettings);
            ApplyWebGLSettings(WebGLSettings);
            ApplyQualitySettings(QualitySettings);
            ApplyAudioSettings(AudioSettings);

            AssetDatabase.SaveAssets();
            Debug.Log($"[MiniGame.Core] Settings restored from snapshot {SnapshotId}.");
        }

        private static string GetSnapshotPath(string snapshotId)
        {
            return Path.Combine(BackupDirectory, $"snapshot_{snapshotId}.json");
        }

        #region Capture / Apply Implementations

        private static void CapturePlayerSettings(Dictionary<string, object> dict)
        {
            dict["scriptingBackend"] = UnityEditor.PlayerSettings.GetScriptingBackend(BuildTargetGroup.WebGL).ToString();
            dict["managedStrippingLevel"] = UnityEditor.PlayerSettings.GetManagedStrippingLevel(BuildTargetGroup.WebGL).ToString();
            dict["bundleVersion"] = UnityEditor.PlayerSettings.bundleVersion;
        }

        private static void ApplyPlayerSettings(Dictionary<string, object> dict)
        {
            if (dict.TryGetValue("scriptingBackend", out var sb))
            {
                if (Enum.TryParse<ScriptingImplementation>(sb.ToString(), out var backend))
                {
                    UnityEditor.PlayerSettings.SetScriptingBackend(BuildTargetGroup.WebGL, backend);
                }
            }

            if (dict.TryGetValue("managedStrippingLevel", out var sl))
            {
                if (Enum.TryParse<ManagedStrippingLevel>(sl.ToString(), out var level))
                {
                    UnityEditor.PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.WebGL, level);
                }
            }

            if (dict.TryGetValue("bundleVersion", out var bv))
            {
                UnityEditor.PlayerSettings.bundleVersion = bv.ToString();
            }
        }

        private static void CaptureWebGLSettings(Dictionary<string, object> dict)
        {
            dict["compressionFormat"] = UnityEditor.PlayerSettings.WebGL.compressionFormat.ToString();
            dict["exceptionSupport"] = UnityEditor.PlayerSettings.WebGL.exceptionSupport.ToString();
            dict["memorySize"] = UnityEditor.PlayerSettings.WebGL.memorySize;
            dict["dataCaching"] = UnityEditor.PlayerSettings.WebGL.dataCaching;
        }

        private static void ApplyWebGLSettings(Dictionary<string, object> dict)
        {
            if (dict.TryGetValue("compressionFormat", out var cf))
            {
                if (Enum.TryParse<WebGLCompressionFormat>(cf.ToString(), out var format))
                {
                    UnityEditor.PlayerSettings.WebGL.compressionFormat = format;
                }
            }

            if (dict.TryGetValue("exceptionSupport", out var es))
            {
                if (Enum.TryParse<WebGLExceptionSupport>(es.ToString(), out var support))
                {
                    UnityEditor.PlayerSettings.WebGL.exceptionSupport = support;
                }
            }

            if (dict.TryGetValue("memorySize", out var ms))
            {
                UnityEditor.PlayerSettings.WebGL.memorySize = (int)ms;
            }

            if (dict.TryGetValue("dataCaching", out var dc))
            {
                UnityEditor.PlayerSettings.WebGL.dataCaching = (bool)dc;
            }
        }

        private static void CaptureQualitySettings(Dictionary<string, object> dict)
        {
            dict["qualityLevelCount"] = UnityEngine.QualitySettings.names.Length;
            dict["currentLevel"] = UnityEngine.QualitySettings.GetQualityLevel();
        }

        private static void ApplyQualitySettings(Dictionary<string, object> dict)
        {
            if (dict.TryGetValue("currentLevel", out var cl))
            {
                UnityEngine.QualitySettings.SetQualityLevel((int)cl);
            }
        }

        private static void CaptureAudioSettings(Dictionary<string, object> dict)
        {
            dict["sampleRate"] = UnityEngine.AudioSettings.GetConfiguration().sampleRate.ToString();
        }

        private static void ApplyAudioSettings(Dictionary<string, object> dict)
        {
            // AudioSettings 的修改通常通过 AudioManager.asset，这里保留占位。
            // 如需修改 DSP buffer size 等，可通过 SerializedObject 操作 AudioManager。
        }

        #endregion
    }
}
