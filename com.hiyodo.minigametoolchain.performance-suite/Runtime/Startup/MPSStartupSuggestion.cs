using System;

namespace MiniGame.PerformanceSuite.Runtime.Startup
{
    /// <summary>
    /// 建议严重程度。
    /// </summary>
    public enum MPSStartupSuggestionSeverity
    {
        Info,
        Warning,
        Critical
    }

    /// <summary>
    /// 启动性能优化建议项。
    /// </summary>
    [Serializable]
    public class MPSStartupSuggestion
    {
        /// <summary>
        /// 严重程度。
        /// </summary>
        public MPSStartupSuggestionSeverity Severity;

        /// <summary>
        /// 建议标题。
        /// </summary>
        public string Title;

        /// <summary>
        /// 建议详情。
        /// </summary>
        public string Description;

        public MPSStartupSuggestion(MPSStartupSuggestionSeverity severity, string title, string description)
        {
            Severity = severity;
            Title = title;
            Description = description;
        }
    }
}
