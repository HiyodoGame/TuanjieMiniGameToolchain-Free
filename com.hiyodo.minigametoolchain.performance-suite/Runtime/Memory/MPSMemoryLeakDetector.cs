using System;
using System.Collections.Generic;
using System.Linq;
using MiniGame.PerformanceSuite.Runtime.Core;
using MiniGame.PerformanceSuite.Runtime.Data;
using UnityEngine;

namespace MiniGame.PerformanceSuite.Runtime.Memory
{
    /// <summary>
    /// 内存泄漏检测器。基于连续快照分析 UnityHeap / JSHeap 增长趋势。
    /// </summary>
    public class MPSMemoryLeakDetector : MonoBehaviour
    {
        private static MPSMemoryLeakDetector _instance;

        [SerializeField]
        private MPSPerformanceConfig _config;

        private readonly Queue<MPSMemorySnapshotData> _recentSnapshots = new Queue<MPSMemorySnapshotData>();
        private MPSMemoryLeakReport _lastReport;
        private double _lastAnalysisTime;

        /// <summary>
        /// 最近一次泄漏检测报告。
        /// </summary>
        public static MPSMemoryLeakReport LastReport => _instance?._lastReport;

        /// <summary>
        /// 是否正在检测。
        /// </summary>
        public static bool IsRunning => _instance != null;

        /// <summary>
        /// 初始化泄漏检测器。
        /// </summary>
        public static void Initialize(MPSPerformanceConfig config)
        {
            if (_instance != null)
            {
                _instance._config = config;
                return;
            }

            var go = new GameObject("MPSMemoryLeakDetector");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<MPSMemoryLeakDetector>();
            _instance._config = config;
        }

        /// <summary>
        /// 喂入一个快照用于趋势分析。
        /// </summary>
        public static void Feed(MPSMemorySnapshotData snapshot)
        {
            if (_instance == null || snapshot == null || _instance._config == null) return;
            _instance.FeedInternal(snapshot);
        }

        /// <summary>
        /// 手动触发一次分析。
        /// </summary>
        public static MPSMemoryLeakReport Analyze()
        {
            if (_instance == null) return null;
            return _instance.AnalyzeInternal();
        }

        /// <summary>
        /// 重置检测状态。
        /// </summary>
        public static void Reset()
        {
            if (_instance == null) return;
            _instance._recentSnapshots.Clear();
            _instance._lastReport = null;
        }

        private void FeedInternal(MPSMemorySnapshotData snapshot)
        {
            _recentSnapshots.Enqueue(snapshot);

            int windowSize = _config.LeakDetectionWindowSize;
            while (_recentSnapshots.Count > windowSize)
            {
                _recentSnapshots.Dequeue();
            }

            double interval = _config.LeakAnalysisIntervalSeconds;
            if (Time.realtimeSinceStartup - _lastAnalysisTime >= interval)
            {
                _lastAnalysisTime = Time.realtimeSinceStartup;
                AnalyzeInternal();
            }
        }

        private MPSMemoryLeakReport AnalyzeInternal()
        {
            if (_recentSnapshots.Count < 3)
            {
                _lastReport = new MPSMemoryLeakReport { HasLeak = false };
                return _lastReport;
            }

            var samples = _recentSnapshots.ToList();
            var unityReport = AnalyzeTrend(samples, "UnityHeap", s => s.UnityHeapBytes);
            var jsReport = AnalyzeTrend(samples, "JsHeap", s => s.JsHeapBytes > 0 ? s.JsHeapBytes : 0);

            // 优先报告增长更严重的类型
            _lastReport = unityReport.GrowthPercent >= jsReport.GrowthPercent ? unityReport : jsReport;

            if (_lastReport.HasLeak)
            {
                if (_config.LogLeakWarning)
                {
                    Debug.LogWarning($"[MPSMemoryLeakDetector] Potential {_lastReport.LeakType} leak detected: " +
                        $"+{FormatBytes(_lastReport.TotalGrowthBytes)} ({_lastReport.GrowthPercent:F1}%) over {_lastReport.Samples.Count} snapshots.");
                }

                MPSAlertDispatcher.AlertMemoryLeak(_lastReport.LeakType, _lastReport.TotalGrowthBytes, _lastReport.GrowthPercent);
            }

            return _lastReport;
        }

        private MPSMemoryLeakReport AnalyzeTrend(List<MPSMemorySnapshotData> samples, string type, Func<MPSMemorySnapshotData, long> selector)
        {
            var report = new MPSMemoryLeakReport
            {
                LeakType = type,
                Samples = new List<MPSMemorySnapshotData>(samples)
            };

            long first = selector(samples.First());
            long last = selector(samples.Last());
            long totalGrowth = last - first;

            report.FirstSnapshot = samples.First();
            report.LastSnapshot = samples.Last();
            report.TotalGrowthBytes = totalGrowth;
            report.AverageGrowthPerSnapshot = samples.Count > 1 ? totalGrowth / (samples.Count - 1) : 0;
            report.GrowthPercent = first > 0 ? (float)totalGrowth / first * 100f : 0f;

            long thresholdBytes = (long)(_config.LeakGrowthThresholdMB * 1024 * 1024);
            float thresholdPercent = _config.LeakGrowthThresholdPercent;

            report.HasLeak = totalGrowth > thresholdBytes && report.GrowthPercent > thresholdPercent;
            return report;
        }

        private static string FormatBytes(long bytes)
        {
            if (Mathf.Abs(bytes) < 1024) return $"{bytes} B";
            if (Mathf.Abs(bytes) < 1024 * 1024) return $"{bytes / 1024f:F1} KB";
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
