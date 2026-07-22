using System;
using System.Collections;
using MiniGame.PerformanceSuite.Runtime.Bridge;
using MiniGame.PerformanceSuite.Runtime.Core;
using MiniGame.PerformanceSuite.Runtime.Data;
using MiniGame.PerformanceSuite.Runtime.Memory;
using MiniGame.PerformanceSuite.Runtime.Startup;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

namespace MiniGame.PerformanceSuite.Runtime.HUD
{
    /// <summary>
    /// 性能面板主控制器。支持自动初始化单例与可拖拽预制体两种集成方式，
    /// 默认使用 uGUI 渲染，uGUI 不可用时自动降级到 IMGUI。
    /// </summary>
    public class MPSPerformanceHUD : MonoBehaviour
    {
        public static MPSPerformanceHUD Instance { get; private set; }

        [SerializeField]
        private MPSPerformanceConfig _config;

        [SerializeField]
        private bool _destroyInReleaseBuilds = true;

        private MPSPerformanceUI _ugui;
        private MPSPerformanceIMGUI _imgui;
        private MPSRingBuffer<MPSFrameSample> _history;
        private float _sampleTimer;
        private float _fpsAccumulator;
        private int _fpsFrameCount;
        private MPSFrameSample _currentSample;
        private MPSFrameSample _averageSample;
        private long _lastGcMemory;
        private long _audioMemoryBytes;
        private float _audioScanTimer;
        private bool _isDestroyed;
        private bool _firstFrameEnded;
        private bool _firstSceneLoadCaptured;
        private float _fpsAlertCooldown;
        private float _heapAlertCooldown;

        public MPSPerformanceConfig Config => _config;
        public MPSFrameSample CurrentSample => _currentSample;
        public MPSFrameSample AverageSample => _averageSample;
        public bool IsVisible { get; private set; }

        /// <summary>
        /// 启动耗时汇总文本。若未记录则返回 null。
        /// </summary>
        public string StartupSummary
        {
            get
            {
                var report = MPSStartupProfiler.GetReport();
                if (report == null || report.Roots == null || report.Roots.Count == 0) return null;
                return $"Startup: {report.TotalStartupTime:F2}s";
            }
        }

        /// <summary>
        /// 自动初始化入口。游戏启动时自动创建 HUD。
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            var config = LoadDefaultConfig();
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<MPSPerformanceConfig>();
            }

            if (!config.AutoInitialize)
            {
                return;
            }

            if (config.AutoFallbackToIMGUI && !IsUGUIAvailable())
            {
                var go = new GameObject("MPSPerformanceHUD");
                var hud = go.AddComponent<MPSPerformanceHUD>();
                hud._config = config;
                return;
            }

            var prefab = Resources.Load<GameObject>("MiniGame.PerformanceSuite/MPSPerformanceHUD");
            if (prefab != null)
            {
                var instance = Instantiate(prefab);
                instance.name = "MPSPerformanceHUD";
                var hud = instance.GetComponent<MPSPerformanceHUD>();
                if (hud != null && hud._config == null)
                {
                    hud._config = config;
                }
            }
            else
            {
                var go = new GameObject("MPSPerformanceHUD");
                var hud = go.AddComponent<MPSPerformanceHUD>();
                hud._config = config;
            }
        }

        private static MPSPerformanceConfig LoadDefaultConfig()
        {
            return Resources.Load<MPSPerformanceConfig>("MiniGame.PerformanceSuite/MPSPerformanceConfig");
        }

        private static bool IsUGUIAvailable()
        {
            return FindObjectOfType<EventSystem>() != null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (_destroyInReleaseBuilds && !Debug.isDebugBuild && !Application.isEditor)
            {
                Destroy(gameObject);
                _isDestroyed = true;
                return;
            }

            if (_config == null)
            {
                _config = LoadDefaultConfig();
            }

            if (_config == null)
            {
                _config = ScriptableObject.CreateInstance<MPSPerformanceConfig>();
            }

            _history = new MPSRingBuffer<MPSFrameSample>(_config.MaxHistorySeconds * _config.SamplesPerSecond);
            _lastGcMemory = GC.GetTotalMemory(false);

            if (_config.ProfileStartup)
            {
                MPSStartupProfiler.Initialize(_config);
            }

            if (_config.TrackResourceLoads)
            {
                MPSResourceLoadTracker.Initialize(_config);
            }

            if (_config.AutoMemorySnapshot)
            {
                MPSMemorySnapshot.Initialize(_config);
            }

            if (_config.EnableLeakDetection)
            {
                MPSMemoryLeakDetector.Initialize(_config);
            }

            MPSAlertDispatcher.Initialize(_config);
        }

        private void Start()
        {
            if (_isDestroyed) return;

            InitializeRenderer();
            IsVisible = _config.VisibleOnStart;
            ApplyVisibility();

            if (_config.ProfileStartup && MPSStartupProfiler.IsRecording)
            {
                MPSStartupProfiler.BeginStage("FirstFrame", "Time to first frame render");
            }
        }

        private void Update()
        {
            if (_isDestroyed) return;

            HandleInput();
            CollectSample();
            TryEndFirstFrame();
            TryAutoMemorySnapshot();
            UpdateAlertCooldowns();
        }

        private void UpdateAlertCooldowns()
        {
            if (_fpsAlertCooldown > 0f) _fpsAlertCooldown -= Time.unscaledDeltaTime;
            if (_heapAlertCooldown > 0f) _heapAlertCooldown -= Time.unscaledDeltaTime;
        }

        private void TryEndFirstFrame()
        {
            if (_firstFrameEnded || !_config.ProfileStartup || !MPSStartupProfiler.IsRecording) return;
            _firstFrameEnded = true;
            MPSStartupProfiler.EndStage("FirstFrame");
            CaptureFirstSceneLoad();
        }

        private void CaptureFirstSceneLoad()
        {
            if (_firstSceneLoadCaptured || !_config.ProfileStartup || !MPSStartupProfiler.IsRecording) return;
            _firstSceneLoadCaptured = true;

            var activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid()) return;

            float threshold = _config != null ? _config.DefaultStartupStageThreshold : 1f;
            MPSStartupProfiler.AddCompletedStage(
                "FirstSceneLoad",
                $"Scene: {activeScene.name}",
                0.0,
                Time.realtimeSinceStartup,
                threshold);
        }

        private void TryAutoMemorySnapshot()
        {
            if (_config == null || !_config.AutoMemorySnapshot) return;
            MPSMemorySnapshot.TryAutoTrigger(_currentSample.UnityHeapBytes, _currentSample.JsHeapBytes);
        }

        private void OnGUI()
        {
            if (_isDestroyed) return;

            if (_imgui != null && (_ugui == null || !_ugui.gameObject.activeInHierarchy))
            {
                _imgui.IsVisible = IsVisible;
            }
        }

        private void InitializeRenderer()
        {
            _ugui = GetComponentInChildren<MPSPerformanceUI>(true);
            _imgui = GetComponent<MPSPerformanceIMGUI>();

            bool useUGUI = _ugui != null && IsUGUIAvailable();

            if (useUGUI)
            {
                _ugui.Setup(_config, this);
                if (_imgui != null)
                {
                    _imgui.enabled = false;
                }
            }
            else if (_config.AutoFallbackToIMGUI)
            {
                if (_imgui == null)
                {
                    _imgui = gameObject.AddComponent<MPSPerformanceIMGUI>();
                }
                _imgui.Setup(_config, this);
                _imgui.enabled = true;

                if (_ugui != null)
                {
                    _ugui.gameObject.SetActive(false);
                }
            }
        }

        private void HandleInput()
        {
            if (Input.GetKeyDown(_config.ToggleKey))
            {
                ToggleVisibility();
            }
        }

        public void ToggleVisibility()
        {
            IsVisible = !IsVisible;
            ApplyVisibility();
        }

        public void SetVisible(bool visible)
        {
            IsVisible = visible;
            ApplyVisibility();
        }

        private void ApplyVisibility()
        {
            if (_ugui != null)
            {
                _ugui.IsVisible = IsVisible;
            }

            if (_imgui != null)
            {
                _imgui.IsVisible = IsVisible;
            }
        }

        private void CollectSample()
        {
            float delta = Time.unscaledDeltaTime;
            _fpsAccumulator += delta;
            _fpsFrameCount++;

            _sampleTimer += delta;
            float sampleInterval = 1f / _config.SamplesPerSecond;

            if (_sampleTimer < sampleInterval) return;
            _sampleTimer -= sampleInterval;

            float avgFps = _fpsFrameCount / Mathf.Max(_fpsAccumulator, 0.0001f);
            _fpsAccumulator = 0f;
            _fpsFrameCount = 0;

            long currentGc = GC.GetTotalMemory(false);
            long gcAlloc = (long)Mathf.Max(0f, currentGc - _lastGcMemory);
            // 托管堆下降说明发生了 GC 回收，标记为 GC 事件
            bool gcEvent = _config.MarkGcEvents && currentGc < _lastGcMemory;
            _lastGcMemory = currentGc;

            // 音频内存低频扫描（FindObjectsOfTypeAll 开销大，不每帧执行）
            if (_config.CaptureAudioMemory)
            {
                _audioScanTimer += sampleInterval;
                if (_audioScanTimer >= _config.AudioMemoryScanIntervalSeconds)
                {
                    _audioScanTimer = 0f;
                    _audioMemoryBytes = MPSAudioMemoryEstimator.EstimateBytes();
                }
            }

            _currentSample = new MPSFrameSample
            {
                Timestamp = Time.realtimeSinceStartup,
                DeltaTime = delta,
                Fps = avgFps,
                UnityHeapBytes = Profiler.GetTotalAllocatedMemoryLong(),
                JsHeapBytes = _config.CaptureJsHeap ? MPSWeChatBridge.GetUsedJSHeapSize() : -1,
                GcAllocBytes = gcAlloc,
                DrawCalls = GetDrawCalls(),
                SetPassCalls = GetSetPassCalls(),
                TriangleCount = GetTriangleCount(),
                AudioMemoryBytes = _config.CaptureAudioMemory ? _audioMemoryBytes : 0,
                GcEventDetected = gcEvent
            };

            _history.Add(_currentSample);
            _averageSample = CalculateAverage();
            DispatchThresholdAlerts(_currentSample);
        }

        private void DispatchThresholdAlerts(MPSFrameSample sample)
        {
            if (_config == null || !_config.EnableAlerts) return;

            if (sample.Fps < _config.FpsWarningThreshold && _fpsAlertCooldown <= 0f)
            {
                _fpsAlertCooldown = _config.FpsAlertCooldownSeconds;
                MPSAlertDispatcher.AlertFps(sample.Fps, _config.FpsWarningThreshold);
            }

            long unityHeapThresholdBytes = (long)(_config.UnityHeapWarningThresholdMB * 1024 * 1024);
            if (sample.UnityHeapBytes > unityHeapThresholdBytes && _heapAlertCooldown <= 0f)
            {
                _heapAlertCooldown = _config.HeapAlertCooldownSeconds;
                MPSAlertDispatcher.AlertUnityHeap(sample.UnityHeapBytes, _config.UnityHeapWarningThresholdMB);
            }

            long jsHeapThresholdBytes = (long)(_config.JsHeapWarningThresholdMB * 1024 * 1024);
            if (sample.JsHeapBytes > 0 && sample.JsHeapBytes > jsHeapThresholdBytes && _heapAlertCooldown <= 0f)
            {
                _heapAlertCooldown = _config.HeapAlertCooldownSeconds;
                MPSAlertDispatcher.AlertJsHeap(sample.JsHeapBytes, _config.JsHeapWarningThresholdMB);
            }
        }

        private MPSFrameSample CalculateAverage()
        {
            if (_history.Count == 0) return _currentSample;

            float avgFps = 0f;
            long avgUnityHeap = 0;
            long avgJsHeap = 0;
            long avgGc = 0;
            int avgDrawCalls = 0;
            int avgSetPass = 0;
            int avgTriangles = 0;

            for (int i = 0; i < _history.Count; i++)
            {
                var s = _history.Get(i);
                avgFps += s.Fps;
                avgUnityHeap += s.UnityHeapBytes;
                avgJsHeap += (long)Mathf.Max(0, s.JsHeapBytes);
                avgGc += s.GcAllocBytes;
                avgDrawCalls += s.DrawCalls;
                avgSetPass += s.SetPassCalls;
                avgTriangles += s.TriangleCount;
            }

            int count = _history.Count;
            return new MPSFrameSample
            {
                Fps = avgFps / count,
                UnityHeapBytes = avgUnityHeap / count,
                JsHeapBytes = avgJsHeap / count,
                GcAllocBytes = avgGc / count,
                DrawCalls = avgDrawCalls / count,
                SetPassCalls = avgSetPass / count,
                TriangleCount = avgTriangles / count
            };
        }

        public MPSFrameSample[] GetHistory()
        {
            return _history?.ToArray() ?? Array.Empty<MPSFrameSample>();
        }

        public void ClearHistory()
        {
            _history?.Clear();
        }

        /// <summary>
        /// 将历史数据导出为 JSON 字符串。
        /// </summary>
        public string ExportHistoryToJson()
        {
            var history = GetHistory();
            var wrapper = new HistoryWrapper { Samples = history };
            return JsonUtility.ToJson(wrapper, true);
        }

        /// <summary>
        /// 将历史数据导出为 CSV 字符串。
        /// </summary>
        public string ExportHistoryToCsv()
        {
            var history = GetHistory();
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Timestamp,Fps,UnityHeapBytes,JsHeapBytes,DrawCalls,SetPassCalls,TriangleCount,GcAllocBytes");
            foreach (var sample in history)
            {
                sb.AppendLine($"{sample.Timestamp:F3},{sample.Fps:F2},{sample.UnityHeapBytes},{sample.JsHeapBytes},{sample.DrawCalls},{sample.SetPassCalls},{sample.TriangleCount},{sample.GcAllocBytes}");
            }
            return sb.ToString();
        }

        [System.Serializable]
        private class HistoryWrapper
        {
            public MPSFrameSample[] Samples;
        }

        /// <summary>
        /// 仅在 Editor 下可获取；真机环境无公开 API，返回 0。
        /// </summary>
        private static int GetDrawCalls()
        {
#if UNITY_EDITOR
            return UnityEditor.UnityStats.drawCalls;
#else
            return 0;
#endif
        }

        /// <summary>
        /// 仅在 Editor 下可获取；真机环境无公开 API，返回 0。
        /// </summary>
        private static int GetSetPassCalls()
        {
#if UNITY_EDITOR
            return UnityEditor.UnityStats.setPassCalls;
#else
            return 0;
#endif
        }

        /// <summary>
        /// 仅在 Editor 下可获取；真机环境无公开 API，返回 0。
        /// </summary>
        private static int GetTriangleCount()
        {
#if UNITY_EDITOR
            return UnityEditor.UnityStats.triangles;
#else
            return 0;
#endif
        }
    }
}
