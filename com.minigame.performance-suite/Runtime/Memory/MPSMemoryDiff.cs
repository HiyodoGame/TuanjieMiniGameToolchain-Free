using System;

namespace MiniGame.PerformanceSuite.Runtime.Memory
{
    /// <summary>
    /// 两个内存快照的 Diff 结果。
    /// </summary>
    [Serializable]
    public class MPSMemoryDiff
    {
        /// <summary>
        /// 基准快照。
        /// </summary>
        public MPSMemorySnapshotData BaseSnapshot;

        /// <summary>
        /// 对比快照。
        /// </summary>
        public MPSMemorySnapshotData ComparedSnapshot;

        /// <summary>
        /// Unity Heap 增量（字节）。
        /// </summary>
        public long UnityHeapDelta;

        /// <summary>
        /// JS Heap 增量（字节）。
        /// </summary>
        public long JsHeapDelta;

        /// <summary>
        /// 未使用预留内存增量（字节）。
        /// </summary>
        public long UnusedReservedMemoryDelta;

        /// <summary>
        /// Mono Heap 增量（字节）。
        /// </summary>
        public long MonoHeapDelta;

        /// <summary>
        /// 计算两个快照的 Diff。
        /// </summary>
        public static MPSMemoryDiff Create(MPSMemorySnapshotData baseSnapshot, MPSMemorySnapshotData comparedSnapshot)
        {
            if (baseSnapshot == null || comparedSnapshot == null)
            {
                throw new ArgumentNullException("Snapshots cannot be null.");
            }

            return new MPSMemoryDiff
            {
                BaseSnapshot = baseSnapshot,
                ComparedSnapshot = comparedSnapshot,
                UnityHeapDelta = comparedSnapshot.UnityHeapBytes - baseSnapshot.UnityHeapBytes,
                JsHeapDelta = comparedSnapshot.JsHeapBytes - baseSnapshot.JsHeapBytes,
                UnusedReservedMemoryDelta = comparedSnapshot.UnusedReservedMemoryBytes - baseSnapshot.UnusedReservedMemoryBytes,
                MonoHeapDelta = comparedSnapshot.MonoHeapBytes - baseSnapshot.MonoHeapBytes
            };
        }
    }
}
