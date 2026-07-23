using System;
using UnityEngine;

namespace MiniGame.PerformanceSuite.Runtime.Data
{
    /// <summary>
    /// 单帧性能采样数据结构。
    /// </summary>
    [Serializable]
    public struct MPSFrameSample
    {
        /// <summary>采样时间戳（秒，基于 Time.realtimeSinceStartup）。</summary>
        public float Timestamp;

        /// <summary>帧间隔（未缩放）。</summary>
        public float DeltaTime;

        /// <summary>1 秒滑动平均 FPS。</summary>
        public float Fps;

        /// <summary>Unity 已分配堆内存（字节）。</summary>
        public long UnityHeapBytes;

        /// <summary>微信 JS 堆内存（字节），-1 表示未获取。</summary>
        public long JsHeapBytes;

        /// <summary>每帧 GC Alloc（字节）。</summary>
        public long GcAllocBytes;

        /// <summary>DrawCall 数量。</summary>
        public int DrawCalls;

        /// <summary>SetPassCall 数量。</summary>
        public int SetPassCalls;

        /// <summary>三角面数。</summary>
        public int TriangleCount;

        /// <summary>音频内存估算（字节），0 表示未采集。</summary>
        public long AudioMemoryBytes;

        /// <summary>本采样周期内是否检测到 GC 触发（托管堆下降）。</summary>
        public bool GcEventDetected;
    }

    /// <summary>
    /// 环形缓冲区，用于保存历史性能采样。
    /// </summary>
    [Serializable]
    public class MPSRingBuffer<T>
    {
        private T[] _buffer;
        private int _head;
        private int _count;

        public int Capacity { get; private set; }
        public int Count => _count;

        public MPSRingBuffer(int capacity)
        {
            Capacity = Mathf.Max(1, capacity);
            _buffer = new T[Capacity];
            _head = 0;
            _count = 0;
        }

        public void Add(T value)
        {
            _buffer[_head] = value;
            _head = (_head + 1) % Capacity;
            if (_count < Capacity)
            {
                _count++;
            }
        }

        public T Get(int index)
        {
            if (index < 0 || index >= _count)
            {
                throw new IndexOutOfRangeException();
            }

            int actualIndex = _count < Capacity
                ? index
                : (_head + index) % Capacity;
            return _buffer[actualIndex];
        }

        public T[] ToArray()
        {
            var result = new T[_count];
            for (int i = 0; i < _count; i++)
            {
                result[i] = Get(i);
            }
            return result;
        }

        public void Clear()
        {
            _count = 0;
            _head = 0;
        }
    }

    /// <summary>
    /// 聚合后的秒级性能数据。
    /// </summary>
    [Serializable]
    public struct MPSSecondSample
    {
        public float Timestamp;
        public float AvgFps;
        public float MinFps;
        public float MaxFps;
        public long AvgUnityHeapBytes;
        public long AvgJsHeapBytes;
        public long AvgGcAllocBytes;
        public int AvgDrawCalls;
        public int AvgSetPassCalls;
        public int AvgTriangleCount;
    }
}
