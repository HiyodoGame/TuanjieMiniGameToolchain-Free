using System;
using System.Collections.Generic;
using System.Linq;

namespace MiniGame.PerformanceSuite.Runtime.Startup
{
    /// <summary>
    /// 启动阶段节点，支持树形嵌套。
    /// </summary>
    [Serializable]
    public class MPSStartupStage
    {
        /// <summary>
        /// 阶段名称。
        /// </summary>
        public string Name;

        /// <summary>
        /// 阶段描述。
        /// </summary>
        public string Description;

        /// <summary>
        /// 阶段开始时间戳（秒，基于 Time.realtimeSinceStartup）。
        /// </summary>
        public double StartTime;

        /// <summary>
        /// 阶段耗时（秒）。负数表示尚未结束。
        /// </summary>
        public double Duration = -1.0;

        /// <summary>
        /// 警告阈值（秒）。超过该值时标记为警告。
        /// </summary>
        public float WarningThreshold = 1f;

        /// <summary>
        /// 子阶段列表。
        /// </summary>
        public List<MPSStartupStage> Children = new List<MPSStartupStage>();

        /// <summary>
        /// 父阶段。根节点为 null。
        /// </summary>
        [NonSerialized]
        public MPSStartupStage Parent;

        /// <summary>
        /// 是否已完成。
        /// </summary>
        public bool IsComplete => Duration >= 0.0;

        /// <summary>
        /// 是否超过警告阈值。
        /// </summary>
        public bool IsWarning => IsComplete && Duration > WarningThreshold;

        /// <summary>
        /// 获取自身与所有子阶段的总耗时（未结束子阶段按 0 计算）。
        /// </summary>
        public double TotalDuration => IsComplete ? Duration + Children.Sum(c => c.TotalDuration) : 0.0;

        public MPSStartupStage(string name, string description = null)
        {
            Name = name;
            Description = description;
        }

        /// <summary>
        /// 添加子阶段。
        /// </summary>
        public MPSStartupStage AddChild(string name, string description = null)
        {
            var child = new MPSStartupStage(name, description) { Parent = this };
            Children.Add(child);
            return child;
        }
    }
}
