using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace MiniGame.Core.Runtime.PerformanceProfiler
{
    /// <summary>
    /// 轻量级运行时性能数据采集器，用于微信小游戏真机环境。
    /// </summary>
    public class PerformanceProfiler : MonoBehaviour
    {
        [Serializable]
        public class FrameSample
        {
            public float Timestamp;
            public float DeltaTime;
            public long UsedHeapBytes;
            public long TotalAllocatedBytes;
            public long GfxDriverBytes;
        }

        public static PerformanceProfiler Instance { get; private set; }

        public int MaxHistoryFrames = 300;
        public bool CaptureOnAwake = true;

        private readonly Queue<FrameSample> _history = new Queue<FrameSample>();
        private bool _isRecording;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (CaptureOnAwake)
            {
                StartRecording();
            }
        }

        private void Update()
        {
            if (!_isRecording) return;

            var sample = new FrameSample
            {
                Timestamp = Time.realtimeSinceStartup,
                DeltaTime = Time.unscaledDeltaTime,
                UsedHeapBytes = Profiler.GetTotalAllocatedMemoryLong(),
                TotalAllocatedBytes = Profiler.GetTotalAllocatedMemoryLong(),
                GfxDriverBytes = Profiler.GetAllocatedMemoryForGraphicsDriver()
            };

            _history.Enqueue(sample);
            while (_history.Count > MaxHistoryFrames)
            {
                _history.Dequeue();
            }
        }

        public void StartRecording()
        {
            _isRecording = true;
        }

        public void StopRecording()
        {
            _isRecording = false;
        }

        public FrameSample[] GetHistory()
        {
            return _history.ToArray();
        }

        public void ClearHistory()
        {
            _history.Clear();
        }
    }
}
