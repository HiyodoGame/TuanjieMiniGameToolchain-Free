using System.Collections.Generic;
using MiniGame.PerformanceSuite.Runtime.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MiniGame.PerformanceSuite.Runtime.Startup
{
    /// <summary>
    /// 资源加载追踪器。
    /// 记录场景、AssetBundle、Resources 等加载事件的开始时间与耗时，
    /// 供编辑器窗口绘制资源加载瀑布图。
    ///
    /// 使用方式：
    /// 1. 自动追踪：初始化后自动记录场景加载完成事件（耗时无法自动获得时记为 0）。
    /// 2. 手动标记：BeginLoad/EndLoad 包裹自定义加载逻辑。
    /// 3. 异步操作：TrackAsyncOperation 传入 AsyncOperation，自动记录完成耗时。
    /// </summary>
    public class MPSResourceLoadTracker : MonoBehaviour
    {
        private const int MaxEvents = 500;

        private static MPSResourceLoadTracker _instance;

        private readonly List<MPSResourceLoadEvent> _events = new List<MPSResourceLoadEvent>();
        private readonly List<MPSResourceLoadEvent> _pending = new List<MPSResourceLoadEvent>();
        private readonly List<AsyncOperation> _pendingOps = new List<AsyncOperation>();
        private readonly List<MPSResourceLoadEvent> _pendingOpEvents = new List<MPSResourceLoadEvent>();

        private MPSPerformanceConfig _config;

        /// <summary>
        /// 当前是否正在追踪。
        /// </summary>
        public static bool IsTracking => _instance != null;

        /// <summary>
        /// 初始化追踪器。由 MPSPerformanceHUD 自动调用，也可手动调用。
        /// </summary>
        public static void Initialize(MPSPerformanceConfig config)
        {
            if (_instance != null)
            {
                _instance._config = config;
                return;
            }

            var go = new GameObject("MPSResourceLoadTracker");
            if (Application.isPlaying)
            {
                // 编辑器（非 Play）模式下调用 DontDestroyOnLoad 会抛异常
                DontDestroyOnLoad(go);
            }

            _instance = go.AddComponent<MPSResourceLoadTracker>();
            _instance._config = config;
            SceneManager.sceneLoaded += _instance.OnSceneLoaded;
        }

        /// <summary>
        /// 销毁追踪器（测试与清理用）。
        /// </summary>
        public static void Shutdown()
        {
            if (_instance == null) return;
            SceneManager.sceneLoaded -= _instance.OnSceneLoaded;
            var go = _instance.gameObject;
            _instance = null;
            if (Application.isPlaying)
            {
                Destroy(go);
            }
            else
            {
                DestroyImmediate(go);
            }
        }

        /// <summary>
        /// 直接记录一条已完成的加载事件。
        /// </summary>
        public static void RecordLoad(string name, string category, double startTime, double duration, long sizeBytes = 0)
        {
            if (_instance == null) return;

            var loadEvent = new MPSResourceLoadEvent
            {
                Name = name,
                Category = category,
                StartTime = startTime,
                Duration = duration,
                SizeBytes = sizeBytes
            };
            _instance.AddEvent(loadEvent);
        }

        /// <summary>
        /// 标记一次加载开始。返回的事件对象用于 EndLoad 配对。
        /// </summary>
        public static MPSResourceLoadEvent BeginLoad(string name, string category)
        {
            if (_instance == null) return null;

            var loadEvent = new MPSResourceLoadEvent
            {
                Name = name,
                Category = category,
                StartTime = Time.realtimeSinceStartup
            };
            _instance._pending.Add(loadEvent);
            return loadEvent;
        }

        /// <summary>
        /// 标记一次加载结束。
        /// </summary>
        public static void EndLoad(MPSResourceLoadEvent loadEvent, long sizeBytes = 0)
        {
            if (_instance == null || loadEvent == null) return;
            if (!_instance._pending.Remove(loadEvent)) return;

            loadEvent.Duration = Time.realtimeSinceStartup - loadEvent.StartTime;
            loadEvent.SizeBytes = sizeBytes;
            _instance.AddEvent(loadEvent);
        }

        /// <summary>
        /// 追踪一个 Unity 异步加载操作（如 AssetBundle.LoadFromFileAsync、SceneManager.LoadSceneAsync），
        /// 完成时自动记录耗时。
        /// </summary>
        public static void TrackAsyncOperation(string name, string category, AsyncOperation operation)
        {
            if (_instance == null || operation == null) return;

            var loadEvent = new MPSResourceLoadEvent
            {
                Name = name,
                Category = category,
                StartTime = Time.realtimeSinceStartup
            };

            if (operation.isDone)
            {
                loadEvent.Duration = 0.0;
                _instance.AddEvent(loadEvent);
                return;
            }

            _instance._pendingOps.Add(operation);
            _instance._pendingOpEvents.Add(loadEvent);
        }

        /// <summary>
        /// 获取所有已完成事件快照。
        /// </summary>
        public static List<MPSResourceLoadEvent> GetEvents()
        {
            if (_instance == null) return new List<MPSResourceLoadEvent>();
            return new List<MPSResourceLoadEvent>(_instance._events);
        }

        /// <summary>
        /// 清除所有已记录事件。
        /// </summary>
        public static void Clear()
        {
            if (_instance == null) return;
            _instance._events.Clear();
            _instance._pending.Clear();
            _instance._pendingOps.Clear();
            _instance._pendingOpEvents.Clear();
        }

        private void Update()
        {
            for (int i = _pendingOps.Count - 1; i >= 0; i--)
            {
                if (!_pendingOps[i].isDone) continue;

                var loadEvent = _pendingOpEvents[i];
                loadEvent.Duration = Time.realtimeSinceStartup - loadEvent.StartTime;
                AddEvent(loadEvent);

                _pendingOps.RemoveAt(i);
                _pendingOpEvents.RemoveAt(i);
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // 场景加载完成时回调，无法获知真实耗时，记录为 0 供瀑布图定位时间点
            AddEvent(new MPSResourceLoadEvent
            {
                Name = scene.name,
                Category = MPSResourceLoadCategory.Scene,
                StartTime = Time.realtimeSinceStartup,
                Duration = 0.0
            });
        }

        private void AddEvent(MPSResourceLoadEvent loadEvent)
        {
            if (_events.Count >= MaxEvents)
            {
                _events.RemoveAt(0);
            }

            _events.Add(loadEvent);

            if (_config != null && _config.LogStartupReport)
            {
                Debug.Log($"[MPSResourceLoadTracker] {loadEvent.Category} '{loadEvent.Name}' loaded in {loadEvent.Duration:F3}s");
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                _instance = null;
            }
        }
    }
}
