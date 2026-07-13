using System;
using System.Collections.Generic;
using System.Linq;
using MiniGame.PerformanceSuite.Runtime.Data;
using UnityEngine;

namespace MiniGame.PerformanceSuite.Runtime.Memory
{
    /// <summary>
    /// 内存快照管理器。提供手动/自动抓取、Diff 计算与报告导出。
    /// </summary>
    public class MPSMemorySnapshot : MonoBehaviour
    {
        private static MPSMemorySnapshot _instance;

        [SerializeField]
        private MPSPerformanceConfig _config;

        private readonly List<MPSMemorySnapshotData> _snapshots = new List<MPSMemorySnapshotData>();
        private readonly HashSet<string> _autoTriggerLabels = new HashSet<string>();
        private float _autoTriggerCooldown;

        /// <summary>
        /// 所有已保存快照。
        /// </summary>
        public static IReadOnlyList<MPSMemorySnapshotData> Snapshots => _instance?._snapshots;

        /// <summary>
        /// 初始化内存快照管理器。
        /// </summary>
        public static void Initialize(MPSPerformanceConfig config)
        {
            if (_instance != null)
            {
                _instance._config = config;
                return;
            }

            var go = new GameObject("MPSMemorySnapshot");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<MPSMemorySnapshot>();
            _instance._config = config;
        }

        /// <summary>
        /// 抓取一个内存快照。
        /// </summary>
        public static MPSMemorySnapshotData Capture(string label)
        {
            if (_instance == null)
            {
                Debug.LogWarning($"[MPSMemorySnapshot] Capture('{label}') called before Initialize. Skipped.");
                return null;
            }

            var snapshot = MPSMemorySnapshotData.Capture(label);
            _instance._snapshots.Add(snapshot);
            MPSMemoryLeakDetector.Feed(snapshot);

            int maxCount = _instance._config != null ? _instance._config.MaxMemorySnapshots : 20;
            while (_instance._snapshots.Count > maxCount)
            {
                _instance._snapshots.RemoveAt(0);
            }

            Debug.Log($"[MPSMemorySnapshot] Captured '{label}': UnityHeap={FormatBytes(snapshot.UnityHeapBytes)}, JSHeap={FormatBytes(snapshot.JsHeapBytes)}");
            return snapshot;
        }

        /// <summary>
        /// 对比两个快照。
        /// </summary>
        public static MPSMemoryDiff Diff(MPSMemorySnapshotData baseSnapshot, MPSMemorySnapshotData comparedSnapshot)
        {
            return MPSMemoryDiff.Create(baseSnapshot, comparedSnapshot);
        }

        /// <summary>
        /// 获取最近一次自动触发快照的时间戳。
        /// </summary>
        public static double? GetLastAutoTriggerTime()
        {
            if (_instance == null || _instance._snapshots.Count == 0) return null;
            var last = _instance._snapshots.LastOrDefault(s => _instance._autoTriggerLabels.Contains(s.Label));
            return last?.Timestamp;
        }

        /// <summary>
        /// 清除所有快照。
        /// </summary>
        public static void Clear()
        {
            if (_instance == null) return;
            _instance._snapshots.Clear();
            _instance._autoTriggerLabels.Clear();
        }

        /// <summary>
        /// 检查并执行自动触发。由 MPSPerformanceHUD 每帧调用。
        /// </summary>
        public static void TryAutoTrigger(long unityHeapBytes, long jsHeapBytes)
        {
            if (_instance == null) return;
            _instance.TryAutoTriggerInternal(unityHeapBytes, jsHeapBytes);
        }

        private void TryAutoTriggerInternal(long unityHeapBytes, long jsHeapBytes)
        {
            if (_config == null || !_config.AutoMemorySnapshot) return;

            _autoTriggerCooldown -= Time.unscaledDeltaTime;
            if (_autoTriggerCooldown > 0f) return;

            bool shouldTrigger = false;
            string reason = null;

            long unityThresholdBytes = (long)(_config.UnityHeapSnapshotThresholdMB * 1024 * 1024);
            if (unityHeapBytes > unityThresholdBytes)
            {
                shouldTrigger = true;
                reason = $"UnityHeap {FormatBytes(unityHeapBytes)} > {FormatBytes(unityThresholdBytes)}";
            }

            long jsThresholdBytes = (long)(_config.JsHeapSnapshotThresholdMB * 1024 * 1024);
            if (jsHeapBytes > 0 && jsHeapBytes > jsThresholdBytes)
            {
                shouldTrigger = true;
                reason = $"JSHeap {FormatBytes(jsHeapBytes)} > {FormatBytes(jsThresholdBytes)}";
            }

            if (!shouldTrigger) return;

            _autoTriggerCooldown = _config.MemorySnapshotCooldownSeconds;
            var snapshot = Capture($"AutoTrigger_{DateTime.Now:HHmmss}");
            if (snapshot != null)
            {
                _autoTriggerLabels.Add(snapshot.Label);
                MPSMemoryLeakDetector.Feed(snapshot);
            }
            Debug.Log($"[MPSMemorySnapshot] Auto triggered: {reason}");
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024f:F1} KB";
            return $"{bytes / (1024f * 1024f):F1} MB";
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
