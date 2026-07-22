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

            // 计算每个类别的评分
            foreach (var summary in report.Categories.Values)
            {
                var categoryIssues = report.Issues.Where(i => (i.Category ?? "General") == summary.Name).ToList();
                summary.Score = Mathf.Clamp(
                    100
                    - categoryIssues.Count(i => i.Severity == IssueSeverity.Error) * 10
                    - categoryIssues.Count(i => i.Severity == IssueSeverity.Warning) * 3,
                    0, 100);
            }

            // 简单总评分：无 Error 60 分起，每类再扣减
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
            sb.AppendLine($"# {EscapeMarkdown(report.ProjectName)} 微信小游戏构建诊断报告");
            sb.AppendLine();
            sb.AppendLine($"- **生成时间**: {report.GeneratedAt}");
            sb.AppendLine($"- **引擎版本**: {EscapeMarkdown(report.EngineVersion)}");
            sb.AppendLine($"- **目标平台**: {EscapeMarkdown(report.TargetPlatform)}");
            sb.AppendLine($"- **综合评分**: {report.OverallScore}/100");
            sb.AppendLine();

            // 执行摘要
            var errorCount = report.Issues.Count(i => i.Severity == IssueSeverity.Error);
            var warningCount = report.Issues.Count(i => i.Severity == IssueSeverity.Warning);
            var infoCount = report.Issues.Count(i => i.Severity == IssueSeverity.Info);
            var totalSavings = report.Issues.Sum(i => Math.Max(0, i.PotentialSavingsBytes));

            sb.AppendLine("## 执行摘要");
            sb.AppendLine();
            sb.AppendLine($"- **问题总数**: {report.Issues.Count}（Error {errorCount} / Warning {warningCount} / Info {infoCount}）");
            sb.AppendLine($"- **预估可节省**: {FormatBytes(totalSavings)}");
            sb.AppendLine($"- **涉及类别**: {report.Categories.Count}");
            sb.AppendLine();

            // 类别汇总
            sb.AppendLine("## 类别汇总");
            sb.AppendLine();
            sb.AppendLine("| 类别 | 问题数 | 预估可节省 | 评分 |");
            sb.AppendLine("|------|--------|------------|------|");
            foreach (var category in report.Categories.Values.OrderByDescending(c => c.TotalPotentialSavingsBytes))
            {
                sb.AppendLine($"| {EscapeMarkdown(category.Name)} | {category.IssueCount} | {FormatBytes(category.TotalPotentialSavingsBytes)} | {category.Score} |");
            }
            sb.AppendLine();

            // 严重级别分布
            sb.AppendLine("## 严重级别分布");
            sb.AppendLine();
            sb.AppendLine("| 严重级别 | 数量 |");
            sb.AppendLine("|----------|------|");
            sb.AppendLine($"| Error | {errorCount} |");
            sb.AppendLine($"| Warning | {warningCount} |");
            sb.AppendLine($"| Info | {infoCount} |");
            sb.AppendLine();

            // 优先处理清单
            var topIssues = report.Issues
                .OrderByDescending(i => i.PotentialSavingsBytes)
                .Take(10)
                .ToList();
            if (topIssues.Count > 0)
            {
                sb.AppendLine("## 优先处理清单（Top 10）");
                sb.AppendLine();
                sb.AppendLine("| 严重级别 | 标题 | 类别 | 预估节省 | 可自动修复 |");
                sb.AppendLine("|----------|------|------|----------|------------|");
                foreach (var issue in topIssues)
                {
                    sb.AppendLine($"| {issue.Severity} | {EscapeMarkdown(issue.Title)} | {EscapeMarkdown(issue.Category)} | {FormatBytes(issue.PotentialSavingsBytes)} | {(issue.AutoFixable ? "是" : "否")} |");
                }
                sb.AppendLine();
            }

            // 自动修复检查清单
            var autoFixableIssues = report.Issues.Where(i => i.AutoFixable).ToList();
            if (autoFixableIssues.Count > 0)
            {
                sb.AppendLine("## 一键修复检查清单");
                sb.AppendLine();
                foreach (var issue in autoFixableIssues)
                {
                    sb.AppendLine($"- [ ] [{issue.Severity}] {EscapeMarkdown(issue.Title)}（{EscapeMarkdown(issue.AssetPath)}）");
                }
                sb.AppendLine();
            }

            // 完整问题列表
            sb.AppendLine("## 完整问题列表");
            sb.AppendLine();
            foreach (var issue in report.Issues)
            {
                sb.AppendLine($"### [{issue.Severity}] {EscapeMarkdown(issue.Title)}");
                sb.AppendLine($"- **规则**: {issue.RuleId}");
                sb.AppendLine($"- **类别**: {EscapeMarkdown(issue.Category)}");
                sb.AppendLine($"- **资产**: `{EscapeMarkdown(issue.AssetPath)}`");
                sb.AppendLine($"- **描述**: {EscapeMarkdown(issue.Description)}");
                sb.AppendLine($"- **建议**: {EscapeMarkdown(issue.SuggestedFix)}");
                sb.AppendLine($"- **预估节省**: {FormatBytes(issue.PotentialSavingsBytes)}");
                sb.AppendLine($"- **可自动修复**: {(issue.AutoFixable ? "是" : "否")}");
                if (!string.IsNullOrEmpty(issue.FixKey))
                {
                    sb.AppendLine($"- **修复命令**: `{issue.FixKey}`");
                }
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
            sb.AppendLine($"<title>{EscapeHtml(report.ProjectName)} 诊断报告</title>");
            sb.AppendLine("<style>body{font-family:sans-serif;max-width:960px;margin:40px auto;}table{border-collapse:collapse;width:100%;}th,td{border:1px solid #ccc;padding:8px;text-align:left;}th{background:#f5f5f5;}.error{color:#d32f2f;}.warning{color:#f57c00;}.info{color:#1976d2;}</style>");
            sb.AppendLine("</head><body>");
            sb.AppendLine($"<h1>{EscapeHtml(report.ProjectName)} 微信小游戏构建诊断报告</h1>");
            sb.AppendLine($"<p>生成时间: {report.GeneratedAt} | 引擎版本: {EscapeHtml(report.EngineVersion)} | 综合评分: <strong>{report.OverallScore}/100</strong></p>");

            sb.AppendLine("<h2>类别汇总</h2><table><tr><th>类别</th><th>问题数</th><th>预估可节省</th></tr>");
            foreach (var category in report.Categories.Values.OrderByDescending(c => c.TotalPotentialSavingsBytes))
            {
                sb.AppendLine($"<tr><td>{EscapeHtml(category.Name)}</td><td>{category.IssueCount}</td><td>{FormatBytes(category.TotalPotentialSavingsBytes)}</td></tr>");
            }
            sb.AppendLine("</table>");

            sb.AppendLine("<h2>问题列表</h2>");
            foreach (var issue in report.Issues)
            {
                sb.AppendLine($"<div class=\"{issue.Severity.ToString().ToLowerInvariant()}\">");
                sb.AppendLine($"<h3>[{issue.Severity}] {EscapeHtml(issue.Title)}</h3>");
                sb.AppendLine($"<p><strong>资产:</strong> {EscapeHtml(issue.AssetPath)}<br/>");
                sb.AppendLine($"<strong>描述:</strong> {EscapeHtml(issue.Description)}<br/>");
                sb.AppendLine($"<strong>建议:</strong> {EscapeHtml(issue.SuggestedFix)}<br/>");
                sb.AppendLine($"<strong>预估节省:</strong> {FormatBytes(issue.PotentialSavingsBytes)}<br/>");
                sb.AppendLine($"<strong>可自动修复:</strong> {(issue.AutoFixable ? "是" : "否")}</p>");
                sb.AppendLine("</div>");
            }

            sb.AppendLine("</body></html>");

            File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
            return outputPath;
        }

        /// <summary>
        /// 导出为 CSV 文件。
        /// </summary>
        public static string ExportCsv(DiagnosticReport report, string outputPath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Severity,Category,RuleId,Title,Description,AssetPath,SuggestedFix,PotentialSavingsBytes,AutoFixable,FixKey");

            foreach (var issue in report.Issues)
            {
                sb.AppendLine(string.Join(",",
                    EscapeCsv(issue.Severity.ToString()),
                    EscapeCsv(issue.Category),
                    EscapeCsv(issue.RuleId),
                    EscapeCsv(issue.Title),
                    EscapeCsv(issue.Description),
                    EscapeCsv(issue.AssetPath),
                    EscapeCsv(issue.SuggestedFix),
                    issue.PotentialSavingsBytes.ToString(),
                    issue.AutoFixable ? "1" : "0",
                    EscapeCsv(issue.FixKey)));
            }

            File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
            return outputPath;
        }

        private static string EscapeMarkdown(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            return input.Replace("|", "\\|").Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");
        }

        private static string EscapeHtml(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            return input
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&#39;");
        }

        private static string EscapeCsv(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            var s = input.Replace("\"", "\"\"");
            if (s.Contains(",") || s.Contains("\n") || s.Contains("\r") || s.Contains("\""))
            {
                s = "\"" + s + "\"";
            }
            return s;
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024f:F2} KB";
            return $"{bytes / (1024f * 1024f):F2} MB";
        }
    }
}
