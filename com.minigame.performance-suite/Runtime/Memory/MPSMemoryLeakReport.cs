using System;
using System.Collections.Generic;

namespace MiniGame.PerformanceSuite.Runtime.Memory
{
    /// <summary>
    /// 内存泄漏检测报告。
    /// </summary>
    [Serializable]
    public class MPSMemoryLeakReport
    {
        /// <summary>
        /// 是否检测到泄漏。
        /// </summary>
        public bool HasLeak;

        /// <summary>
        /// 泄漏类型（UnityHeap / JsHeap / None）。
        /// </summary>
        public string LeakType;

        /// <summary>
        /// 报告生成时间。
        /// </summary>
        public string ReportTime;

        /// <summary>
        /// 检测窗口内首次快照。
        /// </summary>
        public MPSMemorySnapshotData FirstSnapshot;

        /// <summary>
        /// 检测窗口内末次快照。
        /// </summary>
        public MPSMemorySnapshotData LastSnapshot;

        /// <summary>
        /// 总增长量（字节）。
        /// </summary>
        public long TotalGrowthBytes;

        /// <summary>
        /// 平均每次快照增长量（字节）。
        /// </summary>
        public long AverageGrowthPerSnapshot;

        /// <summary>
        /// 增长百分比。
        /// </summary>
        public float GrowthPercent;

        /// <summary>
        /// 检测窗口内所有相关快照。
        /// </summary>
        public List<MPSMemorySnapshotData> Samples;

        public MPSMemoryLeakReport()
        {
            Samples = new List<MPSMemorySnapshotData>();
            ReportTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
}
