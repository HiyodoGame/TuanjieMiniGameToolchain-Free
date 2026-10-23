using System;
using System.Collections.Generic;
using MiniGame.PerformanceSuite.Runtime.Startup;

namespace MiniGame.PerformanceSuite.Runtime.Data
{
    /// <summary>
    /// 启动耗时报告。
    /// </summary>
    [Serializable]
    public class MPSStartupReport
    {
        /// <summary>
        /// 报告生成时间（ISO 8601 字符串）。
        /// </summary>
        public string CaptureTime;

        /// <summary>
        /// 启动总耗时（秒），以最后一个完成根阶段的结束时间为基准。
        /// </summary>
        public double TotalStartupTime;

        /// <summary>
        /// 根阶段列表。
        /// </summary>
        public List<MPSStartupStage> Roots;
    }
}
