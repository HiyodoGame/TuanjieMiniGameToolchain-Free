using System;
using MiniGame.PerformanceSuite.Runtime.Bridge;
using UnityEngine;
using UnityEngine.Profiling;

namespace MiniGame.PerformanceSuite.Runtime.Memory
{
    /// <summary>
    /// 内存快照数据。
    /// </summary>
    [Serializable]
    public class MPSMemorySnapshotData
    {
        /// <summary>
        /// 快照标签。
        /// </summary>
        public string Label;

        /// <summary>
        /// 快照时间戳。
        /// </summary>
        public double Timestamp;

        /// <summary>
        /// Unity 总分配内存（字节）。
        /// </summary>
        public long UnityHeapBytes;

        /// <summary>
        /// JS Heap（字节）。微信平台有效，其他平台为 -1。
        /// </summary>
        public long JsHeapBytes;

        /// <summary>
        /// 未使用的预留内存（字节）。
        /// </summary>
        public long UnusedReservedMemoryBytes;

        /// <summary>
        /// Mono 堆内存（字节）。
        /// </summary>
        public long MonoHeapBytes;

        /// <summary>
        /// 是否包含 JS Heap 数据。
        /// </summary>
        public bool HasJsHeap => JsHeapBytes > 0;

        /// <summary>
        /// 创建并填充一个内存快照。
        /// </summary>
        public static MPSMemorySnapshotData Capture(string label)
        {
            var data = new MPSMemorySnapshotData
            {
                Label = label,
                Timestamp = Time.realtimeSinceStartup,
                UnityHeapBytes = Profiler.GetTotalAllocatedMemoryLong(),
                JsHeapBytes = MPSWeChatBridge.GetUsedJSHeapSize(),
                UnusedReservedMemoryBytes = Profiler.GetTotalUnusedReservedMemoryLong(),
                MonoHeapBytes = Profiler.GetMonoUsedSizeLong()
            };

            return data;
        }
    }
}
