using System;
using System.Collections.Generic;
using System.Linq;
using MiniGame.PerformanceSuite.Runtime.Data;
using UnityEngine;

namespace MiniGame.PerformanceSuite.Runtime.Startup
{
    /// <summary>
    /// 启动耗时拆解器。
    /// 提供 BeginStage/EndStage 静态 API，支持嵌套阶段，并可将结果导出为结构化报告。
    /// </summary>
    public class MPSStartupProfiler : MonoBehaviour
    {
        private static MPSStartupProfiler _instance;

        [SerializeField]
        private MPSPerformanceConfig _config;

        private readonly List<MPSStartupStage> _roots = new List<MPSStartupStage>();
        private readonly Stack<MPSStartupStage> _stack = new Stack<MPSStartupStage>();
        private MPSStartupStage _currentAutoStage;

        /// <summary>
        /// 所有根阶段列表。
        /// </summary>
        public IReadOnlyList<MPSStartupStage> Roots => _roots;

        /// <summary>
        /// 当前配置。
        /// </summary>
        public MPSPerformanceConfig Config => _config;

        /// <summary>
        /// 当前是否正在记录阶段。
        /// </summary>
        public static bool IsRecording => _instance != null;

        /// <summary>
        /// 初始化启动耗时拆解器。由 MPSPerformanceHUD 自动调用，也可手动调用。
        /// </summary>
        public static void Initialize(MPSPerformanceConfig config)
        {
            if (_instance != null)
            {
                _instance._config = config;
                return;
            }

            var go = new GameObject("MPSStartupProfiler");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<MPSStartupProfiler>();
            _instance._config = config;
        }

        /// <summary>
        /// 开始一个阶段。如果当前已有阶段，则作为子阶段嵌入。
        /// </summary>
        public static MPSStartupStage BeginStage(string name, string description = null, float? warningThreshold = null)
        {
            if (_instance == null)
            {
                Debug.LogWarning($"[MPSStartupProfiler] BeginStage('{name}') called before Initialize. Skipped.");
                return null;
            }

            return _instance.BeginStageInternal(name, description, warningThreshold);
        }

        /// <summary>
        /// 结束当前阶段。
        /// </summary>
        public static void EndStage()
        {
            if (_instance == null) return;
            _instance.EndStageInternal();
        }

        /// <summary>
        /// 结束指定名称的阶段。若当前阶段名称不匹配，则逐级向上结束直到匹配。
        /// </summary>
        public static void EndStage(string name)
        {
            if (_instance == null) return;

            while (_instance._stack.Count > 0)
            {
                var stage = _instance._stack.Peek();
                _instance.EndStageInternal();
                if (stage.Name == name) break;
            }
        }

        /// <summary>
        /// 结束所有正在记录的阶段。
        /// </summary>
        public static void EndAllStages()
        {
            if (_instance == null) return;
            while (_instance._stack.Count > 0)
            {
                _instance.EndStageInternal();
            }
        }

        /// <summary>
        /// 手动添加一个已完成的根阶段。用于补充无法通过 Begin/End 捕获的阶段（如初始场景加载）。
        /// </summary>
        public static void AddCompletedStage(string name, string description, double startTime, double duration, float? warningThreshold = null)
        {
            if (_instance == null)
            {
                Debug.LogWarning($"[MPSStartupProfiler] AddCompletedStage('{name}') called before Initialize. Skipped.");
                return;
            }

            var stage = new MPSStartupStage(name, description)
            {
                StartTime = startTime,
                Duration = duration,
                WarningThreshold = warningThreshold ?? _instance.GetDefaultThreshold(name)
            };
            _instance._roots.Add(stage);
        }

        /// <summary>
        /// 获取最近的报告。包含所有已完成根阶段。
        /// </summary>
        public static MPSStartupReport GetReport()
        {
            if (_instance == null) return null;
            return new MPSStartupReport
            {
                CaptureTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                TotalStartupTime = _instance._roots.Count > 0
                    ? _instance._roots.Max(r => r.IsComplete ? r.StartTime + r.Duration : 0.0)
                    : 0.0,
                Roots = new List<MPSStartupStage>(_instance._roots)
            };
        }

        /// <summary>
        /// 清除所有已记录数据。
        /// </summary>
        public static void Clear()
        {
            if (_instance == null) return;
            _instance._roots.Clear();
            _instance._stack.Clear();
        }

        private MPSStartupStage BeginStageInternal(string name, string description, float? warningThreshold)
        {
            var stage = new MPSStartupStage(name, description)
            {
                StartTime = Time.realtimeSinceStartup,
                WarningThreshold = warningThreshold ?? GetDefaultThreshold(name)
            };

            if (_stack.Count > 0)
            {
                var parent = _stack.Peek();
                stage.Parent = parent;
                parent.Children.Add(stage);
            }
            else
            {
                _roots.Add(stage);
            }

            _stack.Push(stage);
            return stage;
        }

        private void EndStageInternal()
        {
            if (_stack.Count == 0)
            {
                Debug.LogWarning("[MPSStartupProfiler] EndStage() called with no active stage.");
                return;
            }

            var stage = _stack.Pop();
            stage.Duration = Time.realtimeSinceStartup - stage.StartTime;

            if (_stack.Count == 0 && _config != null && _config.LogStartupReport)
            {
                Debug.Log($"[MPSStartupProfiler] Stage '{stage.Name}' completed in {stage.Duration:F3}s");
            }
        }

        private float GetDefaultThreshold(string name)
        {
            if (_config == null || _config.StartupStageThresholds == null) return 1f;

            foreach (var threshold in _config.StartupStageThresholds)
            {
                if (threshold.StageName == name)
                {
                    return threshold.ThresholdSeconds;
                }
            }

            return _config.DefaultStartupStageThreshold;
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
