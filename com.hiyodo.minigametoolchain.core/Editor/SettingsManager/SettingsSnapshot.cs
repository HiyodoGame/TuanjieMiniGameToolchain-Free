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

        [SerializeField]
        private List<SettingEntry> playerSettings = new List<SettingEntry>();

        [SerializeField]
        private List<SettingEntry> webglSettings = new List<SettingEntry>();

        [SerializeField]
        private List<SettingEntry> qualitySettings = new List<SettingEntry>();

        [SerializeField]
        private List<SettingEntry> audioSettings = new List<SettingEntry>();

        [Serializable]
        public class SettingEntry
        {
            public string Key;
            public string Value;
        }

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

            CapturePlayerSettings(snapshot.playerSettings);
            CaptureWebGLSettings(snapshot.webglSettings);
            CaptureQualitySettings(snapshot.qualitySettings);
            CaptureAudioSettings(snapshot.audioSettings);

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
        /// 删除指定 ID 的快照。
        /// </summary>
        public static bool Delete(string snapshotId)
        {
            var path = GetSnapshotPath(snapshotId);
            if (!File.Exists(path)) return false;
            File.Delete(path);
            return true;
        }

        /// <summary>
        /// 应用快照中的设置（回滚）。
        /// </summary>
        public void Restore()
        {
            ApplyPlayerSettings(playerSettings);
            ApplyWebGLSettings(webglSettings);
            ApplyQualitySettings(qualitySettings);
            ApplyAudioSettings(audioSettings);

            AssetDatabase.SaveAssets();
            Debug.Log($"[MiniGame.Core] Settings restored from snapshot {SnapshotId}.");
        }

        private static string GetSnapshotPath(string snapshotId)
        {
            return Path.Combine(BackupDirectory, $"snapshot_{snapshotId}.json");
        }

        #region Capture / Apply Implementations

        private static void SetEntry(List<SettingEntry> list, string key, string value)
        {
            var existing = list.Find(e => e.Key == key);
            if (existing != null)
            {
                existing.Value = value;
            }
            else
            {
                list.Add(new SettingEntry { Key = key, Value = value });
            }
        }

        private static bool TryGetEntry(List<SettingEntry> list, string key, out string value)
        {
            var entry = list.Find(e => e.Key == key);
            if (entry != null)
            {
                value = entry.Value;
                return true;
            }

            value = null;
            return false;
        }

        private static void CapturePlayerSettings(List<SettingEntry> list)
        {
            SetEntry(list, "scriptingBackend", UnityEditor.PlayerSettings.GetScriptingBackend(BuildTargetGroup.WebGL).ToString());
            SetEntry(list, "managedStrippingLevel", UnityEditor.PlayerSettings.GetManagedStrippingLevel(BuildTargetGroup.WebGL).ToString());
            SetEntry(list, "bundleVersion", UnityEditor.PlayerSettings.bundleVersion);
            SetEntry(list, "stripEngineCode", UnityEditor.PlayerSettings.stripEngineCode.ToString());
        }

        private static void ApplyPlayerSettings(List<SettingEntry> list)
        {
            if (TryGetEntry(list, "scriptingBackend", out var sb) && Enum.TryParse<ScriptingImplementation>(sb, out var backend))
            {
                UnityEditor.PlayerSettings.SetScriptingBackend(BuildTargetGroup.WebGL, backend);
            }

            if (TryGetEntry(list, "managedStrippingLevel", out var sl) && Enum.TryParse<ManagedStrippingLevel>(sl, out var level))
            {
                UnityEditor.PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.WebGL, level);
            }

            if (TryGetEntry(list, "bundleVersion", out var bv))
            {
                UnityEditor.PlayerSettings.bundleVersion = bv;
            }

            if (TryGetEntry(list, "stripEngineCode", out var sec) && bool.TryParse(sec, out var stripEngineCode))
            {
                UnityEditor.PlayerSettings.stripEngineCode = stripEngineCode;
            }
        }

        private static void CaptureWebGLSettings(List<SettingEntry> list)
        {
            SetEntry(list, "compressionFormat", UnityEditor.PlayerSettings.WebGL.compressionFormat.ToString());
            SetEntry(list, "exceptionSupport", UnityEditor.PlayerSettings.WebGL.exceptionSupport.ToString());
            SetEntry(list, "memorySize", UnityEditor.PlayerSettings.WebGL.memorySize.ToString());
            SetEntry(list, "dataCaching", UnityEditor.PlayerSettings.WebGL.dataCaching.ToString());
            SetEntry(list, "nameFilesAsHashes", UnityEditor.PlayerSettings.WebGL.nameFilesAsHashes.ToString());
            SetEntry(list, "debugSymbolMode", UnityEditor.PlayerSettings.WebGL.debugSymbolMode.ToString());

            var wasmCodeSplit = GetWasmCodeSplit();
            if (wasmCodeSplit.HasValue)
            {
                SetEntry(list, "wasmCodeSplit", wasmCodeSplit.Value.ToString());
            }
        }

        private static void ApplyWebGLSettings(List<SettingEntry> list)
        {
            if (TryGetEntry(list, "compressionFormat", out var cf) && Enum.TryParse<WebGLCompressionFormat>(cf, out var format))
            {
                UnityEditor.PlayerSettings.WebGL.compressionFormat = format;
            }

            if (TryGetEntry(list, "exceptionSupport", out var es) && Enum.TryParse<WebGLExceptionSupport>(es, out var support))
            {
                UnityEditor.PlayerSettings.WebGL.exceptionSupport = support;
            }

            if (TryGetEntry(list, "memorySize", out var ms) && int.TryParse(ms, out var memorySize))
            {
                UnityEditor.PlayerSettings.WebGL.memorySize = memorySize;
            }

            if (TryGetEntry(list, "dataCaching", out var dc) && bool.TryParse(dc, out var dataCaching))
            {
                UnityEditor.PlayerSettings.WebGL.dataCaching = dataCaching;
            }

            if (TryGetEntry(list, "nameFilesAsHashes", out var nfah) && bool.TryParse(nfah, out var nameFilesAsHashes))
            {
                UnityEditor.PlayerSettings.WebGL.nameFilesAsHashes = nameFilesAsHashes;
            }

            if (TryGetEntry(list, "debugSymbolMode", out var dsm) && Enum.TryParse<WebGLDebugSymbolMode>(dsm, out var debugSymbolMode))
            {
                UnityEditor.PlayerSettings.WebGL.debugSymbolMode = debugSymbolMode;
            }

            if (TryGetEntry(list, "wasmCodeSplit", out var wcs) && bool.TryParse(wcs, out var wasmCodeSplitValue))
            {
                ApplyWasmCodeSplit(wasmCodeSplitValue);
            }
        }

        private static void CaptureQualitySettings(List<SettingEntry> list)
        {
            SetEntry(list, "qualityLevelCount", UnityEngine.QualitySettings.names.Length.ToString());
            SetEntry(list, "currentLevel", UnityEngine.QualitySettings.GetQualityLevel().ToString());
        }

        private static void ApplyQualitySettings(List<SettingEntry> list)
        {
            if (TryGetEntry(list, "currentLevel", out var cl) && int.TryParse(cl, out var currentLevel))
            {
                UnityEngine.QualitySettings.SetQualityLevel(currentLevel);
            }
        }

        private static void CaptureAudioSettings(List<SettingEntry> list)
        {
            SetEntry(list, "sampleRate", UnityEngine.AudioSettings.GetConfiguration().sampleRate.ToString());
        }

        private static void ApplyAudioSettings(List<SettingEntry> list)
        {
            // AudioSettings 的修改通常通过 AudioManager.asset，这里保留占位。
            // 如需修改 DSP buffer size 等，可通过 SerializedObject 操作 AudioManager。
        }

        private static bool? GetWasmCodeSplit()
        {
            try
            {
                var webglSettingsType = typeof(PlayerSettings.WebGL);
                var prop = webglSettingsType.GetProperty("wasmCodeSplit",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                if (prop != null)
                {
                    return (bool)prop.GetValue(null);
                }
            }
            catch
            {
                // 属性不存在或无法读取时忽略
            }
            return null;
        }

        private static void ApplyWasmCodeSplit(bool enable)
        {
            try
            {
                var webglSettingsType = typeof(PlayerSettings.WebGL);
                var prop = webglSettingsType.GetProperty("wasmCodeSplit",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                if (prop != null)
                {
                    prop.SetValue(null, enable);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MiniGame.Core] 无法设置 wasmCodeSplit: {ex.Message}");
            }
        }

        #endregion
    }
}
