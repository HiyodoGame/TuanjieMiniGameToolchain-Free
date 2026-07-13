using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MiniGame.Core.Editor.Analyzers;
using UnityEditor;
using UnityEngine;

namespace MiniGame.Core.Editor.ReportGenerators
{
    /// <summary>
    /// 诊断报告数据结构。
    /// </summary>
    [Serializable]
    public class DiagnosticReport
    {
        public string ProjectName;
        public string GeneratedAt;
        public string EngineVersion;
        public string TargetPlatform;
        public int OverallScore;
        public List<DiagnosticIssue> Issues = new List<DiagnosticIssue>();
        public Dictionary<string, CategorySummary> Categories = new Dictionary<string, CategorySummary>();

        public DiagnosticReport()
        {
            ProjectName = Application.productName;
            GeneratedAt = DateTime.Now.ToString("O");
            EngineVersion = Application.unityVersion;
            TargetPlatform = "WeChatMiniGame";
        }
    }

    /// <summary>
    /// 按类别聚合的统计信息。
    /// </summary>
    [Serializable]
    public class CategorySummary
    {
        public string Name;
        public int IssueCount;
        public long TotalPotentialSavingsBytes;
        public int Score;
    }

    /// <summary>
    /// 报告生成器：支持 Markdown 与 HTML 格式导出。
    /// </summary>
    public static class ReportGenerator
    {
        /// <summary>
        /// 根据问题列表生成结构化报告。
        /// </summary>
        public static DiagnosticReport GenerateReport(List<DiagnosticIssue> issues)
        {
            var report = new DiagnosticReport();
            report.Issues = issues ?? new List<DiagnosticIssue>();

            foreach (var issue in report.Issues)
            {
                var category = issue.Category ?? "General";
                if (!report.Categories.TryGetValue(category, out var summary))
                {
                    summary = new CategorySummary { Name = category };
                    report.Categories[category] = summary;
                }

                summary.IssueCount++;
                summary.TotalPotentialSavingsBytes += Math.Max(0, issue.PotentialSavingsBytes);
            }

            // 简单评分：无 Error 60 分起，每类再扣减
            report.OverallScore = Mathf.Clamp(100 - report.Issues.Count(i => i.Severity == IssueSeverity.Error) * 10
                - report.Issues.Count(i => i.Severity == IssueSeverity.Warning) * 3, 0, 100);

            return report;
        }

        /// <summary>
        /// 导出为 Markdown 文件。
        /// </summary>
        public static string ExportMarkdown(DiagnosticReport report, string outputPath)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# {report.ProjectName} 微信小游戏构建诊断报告");
            sb.AppendLine();
            sb.AppendLine($"- **生成时间**: {report.GeneratedAt}");
            sb.AppendLine($"- **引擎版本**: {report.EngineVersion}");
            sb.AppendLine($"- **目标平台**: {report.TargetPlatform}");
            sb.AppendLine($"- **综合评分**: {report.OverallScore}/100");
            sb.AppendLine();

            sb.AppendLine("## 类别汇总");
            sb.AppendLine();
            sb.AppendLine("| 类别 | 问题数 | 预估可节省 | 评分 |");
            sb.AppendLine("|------|--------|------------|------|");
            foreach (var category in report.Categories.Values.OrderByDescending(c => c.TotalPotentialSavingsBytes))
            {
                sb.AppendLine($"| {category.Name} | {category.IssueCount} | {FormatBytes(category.TotalPotentialSavingsBytes)} | {category.Score} |");
            }
            sb.AppendLine();

            sb.AppendLine("## 问题列表");
            sb.AppendLine();
            foreach (var issue in report.Issues)
            {
                sb.AppendLine($"### [{issue.Severity}] {issue.Title}");
                sb.AppendLine($"- **规则**: {issue.RuleId}");
                sb.AppendLine($"- **类别**: {issue.Category}");
                sb.AppendLine($"- **资产**: {issue.AssetPath}");
                sb.AppendLine($"- **描述**: {issue.Description}");
                sb.AppendLine($"- **建议**: {issue.SuggestedFix}");
                sb.AppendLine($"- **预估节省**: {FormatBytes(issue.PotentialSavingsBytes)}");
                sb.AppendLine();
            }

            File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
            return outputPath;
        }

        /// <summary>
        /// 导出为 HTML 文件。
        /// </summary>
        public static string ExportHtml(DiagnosticReport report, string outputPath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html><head><meta charset=\"utf-8\">");
            sb.AppendLine($"<title>{report.ProjectName} 诊断报告</title>");
            sb.AppendLine("<style>body{font-family:sans-serif;max-width:960px;margin:40px auto;}table{border-collapse:collapse;width:100%;}th,td{border:1px solid #ccc;padding:8px;text-align:left;}th{background:#f5f5f5;}.error{color:#d32f2f;}.warning{color:#f57c00;}.info{color:#1976d2;}</style>");
            sb.AppendLine("</head><body>");
            sb.AppendLine($"<h1>{report.ProjectName} 微信小游戏构建诊断报告</h1>");
            sb.AppendLine($"<p>生成时间: {report.GeneratedAt} | 引擎版本: {report.EngineVersion} | 综合评分: <strong>{report.OverallScore}/100</strong></p>");

            sb.AppendLine("<h2>类别汇总</h2><table><tr><th>类别</th><th>问题数</th><th>预估可节省</th></tr>");
            foreach (var category in report.Categories.Values.OrderByDescending(c => c.TotalPotentialSavingsBytes))
            {
                sb.AppendLine($"<tr><td>{category.Name}</td><td>{category.IssueCount}</td><td>{FormatBytes(category.TotalPotentialSavingsBytes)}</td></tr>");
            }
            sb.AppendLine("</table>");

            sb.AppendLine("<h2>问题列表</h2>");
            foreach (var issue in report.Issues)
            {
                sb.AppendLine($"<div class=\"{issue.Severity.ToString().ToLowerInvariant()}\">");
                sb.AppendLine($"<h3>[{issue.Severity}] {issue.Title}</h3>");
                sb.AppendLine($"<p><strong>资产:</strong> {issue.AssetPath}<br/>");
                sb.AppendLine($"<strong>描述:</strong> {issue.Description}<br/>");
                sb.AppendLine($"<strong>建议:</strong> {issue.SuggestedFix}<br/>");
                sb.AppendLine($"<strong>预估节省:</strong> {FormatBytes(issue.PotentialSavingsBytes)}</p>");
                sb.AppendLine("</div>");
            }

            sb.AppendLine("</body></html>");

            File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
            return outputPath;
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024f:F2} KB";
            return $"{bytes / (1024f * 1024f):F2} MB";
        }
    }
}
