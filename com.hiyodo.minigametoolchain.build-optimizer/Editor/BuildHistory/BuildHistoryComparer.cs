using System.Collections.Generic;
using System.Linq;
using MiniGame.Core.Editor.Analyzers;

namespace MiniGame.BuildOptimizer.Editor.BuildHistory
{
    /// <summary>
    /// 两次诊断历史对比结果。
    /// </summary>
    public class BuildHistoryComparison
    {
        public BuildHistoryEntry Baseline;
        public BuildHistoryEntry Current;

        public int ScoreDelta => Current.OverallScore - Baseline.OverallScore;

        public List<CategoryDelta> CategoryDeltas = new List<CategoryDelta>();
        public List<IssueDiff> AddedIssues = new List<IssueDiff>();
        public List<IssueDiff> RemovedIssues = new List<IssueDiff>();
        public List<IssueDiff> UnchangedIssues = new List<IssueDiff>();
    }

    public class CategoryDelta
    {
        public string Name;
        public int IssueCountDelta;
        public long SavingsBytesDelta;
        public int ScoreDelta;
        public int BaselineIssueCount;
        public int CurrentIssueCount;
        public long BaselineSavingsBytes;
        public long CurrentSavingsBytes;
        public int BaselineScore;
        public int CurrentScore;
    }

    public class IssueDiff
    {
        public string RuleId;
        public string Category;
        public IssueSeverity Severity;
        public string Title;
        public string AssetPath;
        public long PotentialSavingsBytes;
    }

    /// <summary>
    /// 对比两次诊断历史。
    /// </summary>
    public static class BuildHistoryComparer
    {
        public static BuildHistoryComparison Compare(BuildHistoryEntry baseline, BuildHistoryEntry current)
        {
            var comparison = new BuildHistoryComparison
            {
                Baseline = baseline,
                Current = current
            };

            CompareCategories(comparison);
            CompareIssues(comparison);

            return comparison;
        }

        private static void CompareCategories(BuildHistoryComparison comparison)
        {
            var baselineCategories = comparison.Baseline.Categories?.ToDictionary(c => c.Name) ?? new Dictionary<string, BuildHistoryCategoryEntry>();
            var currentCategories = comparison.Current.Categories?.ToDictionary(c => c.Name) ?? new Dictionary<string, BuildHistoryCategoryEntry>();
            var allNames = new HashSet<string>(baselineCategories.Keys);
            allNames.UnionWith(currentCategories.Keys);

            foreach (var name in allNames)
            {
                baselineCategories.TryGetValue(name, out var baseline);
                currentCategories.TryGetValue(name, out var current);

                comparison.CategoryDeltas.Add(new CategoryDelta
                {
                    Name = name,
                    BaselineIssueCount = baseline?.IssueCount ?? 0,
                    CurrentIssueCount = current?.IssueCount ?? 0,
                    IssueCountDelta = (current?.IssueCount ?? 0) - (baseline?.IssueCount ?? 0),
                    BaselineSavingsBytes = baseline?.TotalPotentialSavingsBytes ?? 0,
                    CurrentSavingsBytes = current?.TotalPotentialSavingsBytes ?? 0,
                    SavingsBytesDelta = (current?.TotalPotentialSavingsBytes ?? 0) - (baseline?.TotalPotentialSavingsBytes ?? 0),
                    BaselineScore = baseline?.Score ?? 100,
                    CurrentScore = current?.Score ?? 100,
                    ScoreDelta = (current?.Score ?? 100) - (baseline?.Score ?? 100)
                });
            }
        }

        private static void CompareIssues(BuildHistoryComparison comparison)
        {
            var baselineIssues = (comparison.Baseline.Issues ?? new BuildHistoryIssueEntry[0])
                .Select(ToDiff)
                .ToList();
            var currentIssues = (comparison.Current.Issues ?? new BuildHistoryIssueEntry[0])
                .Select(ToDiff)
                .ToList();

            var baselineKeys = new HashSet<string>(baselineIssues.Select(GetIssueKey));
            var currentKeys = new HashSet<string>(currentIssues.Select(GetIssueKey));

            comparison.AddedIssues = currentIssues.Where(i => !baselineKeys.Contains(GetIssueKey(i))).ToList();
            comparison.RemovedIssues = baselineIssues.Where(i => !currentKeys.Contains(GetIssueKey(i))).ToList();
            comparison.UnchangedIssues = currentIssues.Where(i => baselineKeys.Contains(GetIssueKey(i))).ToList();
        }

        private static IssueDiff ToDiff(BuildHistoryIssueEntry issue)
        {
            return new IssueDiff
            {
                RuleId = issue.RuleId,
                Category = issue.Category,
                Severity = issue.Severity,
                Title = issue.Title,
                AssetPath = issue.AssetPath,
                PotentialSavingsBytes = issue.PotentialSavingsBytes
            };
        }

        private static string GetIssueKey(IssueDiff issue)
        {
            return $"{issue.RuleId}|{issue.Title}|{issue.AssetPath}";
        }
    }
}
