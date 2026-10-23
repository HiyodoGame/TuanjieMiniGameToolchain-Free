using System;

namespace MiniGame.PerformanceSuite.Runtime.Core
{
    /// <summary>
    /// 告警级别。
    /// </summary>
    public enum MPSAlertLevel
    {
        Info,
        Warning,
        Error
    }

    /// <summary>
    /// 告警消息。
    /// </summary>
    [Serializable]
    public class MPSAlertMessage
    {
        /// <summary>
        /// 告警标题。
        /// </summary>
        public string Title;

        /// <summary>
        /// 告警内容。
        /// </summary>
        public string Body;

        /// <summary>
        /// 告警级别。
        /// </summary>
        public MPSAlertLevel Level;

        /// <summary>
        /// 告警类型标签（如 FPS / Heap / Leak）。
        /// </summary>
        public string Category;

        /// <summary>
        /// 触发时间。
        /// </summary>
        public string Timestamp;

        public MPSAlertMessage(string title, string body, MPSAlertLevel level, string category)
        {
            Title = title;
            Body = body;
            Level = level;
            Category = category;
            Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
}
