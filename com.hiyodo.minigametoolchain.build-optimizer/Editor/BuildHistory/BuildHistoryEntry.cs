using System;
using MiniGame.Core.Editor.Analyzers;

namespace MiniGame.BuildOptimizer.Editor.BuildHistory
{
    /// <summary>
    /// 单次诊断历史的类别摘要记录。
    /// </summary>
    [Serializable]
    public class BuildHistoryCategoryEntry
    {
        public string Name;
        public int IssueCount;
        public long TotalPotentialSavingsBytes;
        public int Score;
    }

    /// <summary>
    /// 单次诊断历史的问题记录（用于 diff）。
    /// </summary>
    [Serializable]
    public class BuildHistoryIssueEntry
    {
        public string RuleId;
        public string Category;
        public IssueSeverity Severity;
        public string Title;
        public string AssetPath;
        public long PotentialSavingsBytes;
    }

    /// <summary>
    /// 单次诊断历史条目。
    /// </summary>
    [Serializable]
    public class BuildHistoryEntry
    {
        public string Id;
        public string CreatedAt;
        public string Comment;
        public int OverallScore;
        public BuildHistoryCategoryEntry[] Categories;
        public BuildHistoryIssueEntry[] Issues;

        public static BuildHistoryIssueEntry FromDiagnosticIssue(DiagnosticIssue issue)
        {
            return new BuildHistoryIssueEntry
            {
                RuleId = issue.RuleId,
                Category = issue.Category,
                Severity = issue.Severity,
                Title = issue.Title,
                AssetPath = issue.AssetPath,
                PotentialSavingsBytes = issue.PotentialSavingsBytes
            };
        }
    }
}
