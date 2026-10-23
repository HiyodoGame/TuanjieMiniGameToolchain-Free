using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MiniGame.BuildOptimizer.Editor.BuildHistory;
using MiniGame.Core.Editor.Analyzers;
using MiniGame.Core.Editor.ReportGenerators;
using UnityEngine;

namespace MiniGame.BuildOptimizer.Editor.ReportGenerators
{
    /// <summary>
    /// Build Optimizer 可视化 HTML 报告生成器。
    /// 生成自包含 HTML，包含评分仪表盘、类别条形图、严重级别分布、Top 问题与历史趋势。
    /// </summary>
    public static class BuildOptimizerHtmlReport
    {
        public static void Export(DiagnosticReport report, string outputPath)
        {
            Export(report, outputPath, BuildHistoryStorage.ListIds());
        }

        public static void Export(DiagnosticReport report, string outputPath, List<string> historyIds)
        {
            var history = new List<BuildHistoryEntry>();
            if (historyIds != null)
            {
                foreach (var id in historyIds.Take(20).Reverse())
                {
                    var entry = BuildHistoryStorage.Load(id);
                    if (entry != null) history.Add(entry);
                }
            }

            var html = Generate(report, history);
            File.WriteAllText(outputPath, html, Encoding.UTF8);
        }

        private static string Generate(DiagnosticReport report, List<BuildHistoryEntry> history)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"zh-CN\">");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset=\"utf-8\">");
            sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
            sb.AppendLine($"<title>{EscapeHtml(report.ProjectName)} 微信小游戏构建诊断报告</title>");
            sb.AppendLine("<style>");
            sb.AppendLine(GetCss());
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");

            DrawHeader(sb, report);
            DrawSummary(sb, report);
            DrawExecutiveSummary(sb, report);
            DrawCategoryChart(sb, report);
            DrawSeverityChart(sb, report);
            DrawTopIssues(sb, report);
            DrawHistoryTrend(sb, history);
            DrawFullIssueList(sb, report);

            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
            return sb.ToString();
        }

        private static void DrawHeader(StringBuilder sb, DiagnosticReport report)
        {
            sb.AppendLine("<div class='card header'>");
            sb.AppendLine($"<div class='gauge'>{RenderScoreGauge(report.OverallScore)}</div>");
            sb.AppendLine("<div class='header-info'>");
            sb.AppendLine($"<h1>{EscapeHtml(report.ProjectName)} 微信小游戏构建诊断报告</h1>");
            sb.AppendLine($"<p class='meta'>生成时间: {report.GeneratedAt} &nbsp;|&nbsp; 引擎版本: {EscapeHtml(report.EngineVersion)} &nbsp;|&nbsp; 目标平台: {EscapeHtml(report.TargetPlatform)}</p>");
            sb.AppendLine($"<div class='score-value {GetScoreClass(report.OverallScore)}'>综合评分 {report.OverallScore}/100</div>");
            sb.AppendLine("</div>");
            sb.AppendLine("</div>");
        }

        private static void DrawSummary(StringBuilder sb, DiagnosticReport report)
        {
            var errorCount = report.Issues.Count(i => i.Severity == IssueSeverity.Error);
            var warningCount = report.Issues.Count(i => i.Severity == IssueSeverity.Warning);
            var infoCount = report.Issues.Count(i => i.Severity == IssueSeverity.Info);
            var totalSavings = report.Issues.Sum(i => Math.Max(0, i.PotentialSavingsBytes));

            sb.AppendLine("<div class='card'>");
            sb.AppendLine("<h2>概览</h2>");
            sb.AppendLine("<div class='summary-grid'>");
            sb.AppendLine($"<div class='summary-item'><div class='value'>{report.Issues.Count}</div><div class='label'>问题总数</div></div>");
            sb.AppendLine($"<div class='summary-item error'><div class='value'>{errorCount}</div><div class='label'>Error</div></div>");
            sb.AppendLine($"<div class='summary-item warning'><div class='value'>{warningCount}</div><div class='label'>Warning</div></div>");
            sb.AppendLine($"<div class='summary-item info'><div class='value'>{infoCount}</div><div class='label'>Info</div></div>");
            sb.AppendLine($"<div class='summary-item'><div class='value'>{FormatBytes(totalSavings)}</div><div class='label'>预估可节省</div></div>");
            sb.AppendLine($"<div class='summary-item'><div class='value'>{report.Categories.Count}</div><div class='label'>涉及类别</div></div>");
            sb.AppendLine("</div>");
            sb.AppendLine("</div>");
        }

        private static void DrawExecutiveSummary(StringBuilder sb, DiagnosticReport report)
        {
            var top = report.Issues
                .OrderByDescending(i => i.PotentialSavingsBytes)
                .Take(5)
                .ToList();

            var autoFixableCount = report.Issues.Count(i => i.AutoFixable);

            sb.AppendLine("<div class='card'>");
            sb.AppendLine("<h2>执行摘要 & 优先处理建议</h2>");
            sb.AppendLine($"<p>本次诊断共发现 <strong>{report.Issues.Count}</strong> 个问题，其中 <strong>{autoFixableCount}</strong> 个可通过 Build Optimizer 一键自动修复。建议优先处理以下 Top 5 问题：</p>");

            if (top.Count > 0)
            {
                sb.AppendLine("<table class='issue-table'>");
                sb.AppendLine("<thead><tr><th>严重级别</th><th>标题</th><th>类别</th><th>预估节省</th><th>可自动修复</th></tr></thead>");
                sb.AppendLine("<tbody>");
                foreach (var issue in top)
                {
                    sb.AppendLine("<tr>");
                    sb.AppendLine($"<td><span class='tag {issue.Severity.ToString().ToLowerInvariant()}'>{issue.Severity}</span></td>");
                    sb.AppendLine($"<td>{EscapeHtml(issue.Title)}</td>");
                    sb.AppendLine($"<td>{EscapeHtml(issue.Category)}</td>");
                    sb.AppendLine($"<td>{FormatBytes(issue.PotentialSavingsBytes)}</td>");
                    sb.AppendLine($"<td>{(issue.AutoFixable ? "✅ 是" : "—")}</td>");
                    sb.AppendLine("</tr>");
                }
                sb.AppendLine("</tbody></table>");
            }
            else
            {
                sb.AppendLine("<p>当前没有检测到问题。</p>");
            }

            sb.AppendLine("</div>");
        }

        private static void DrawCategoryChart(StringBuilder sb, DiagnosticReport report)
        {
            var categories = report.Categories.Values.OrderByDescending(c => c.TotalPotentialSavingsBytes).ToList();
            var maxSavings = categories.Count > 0 ? categories.Max(c => c.TotalPotentialSavingsBytes) : 1;

            sb.AppendLine("<div class='card'>");
            sb.AppendLine("<h2>类别分析</h2>");
            sb.AppendLine("<table class='category-table'>");
            sb.AppendLine("<thead><tr><th>类别</th><th>问题数</th><th>预估可节省</th><th>节省占比</th><th>评分</th></tr></thead>");
            sb.AppendLine("<tbody>");

            foreach (var cat in categories)
            {
                var savingsPercent = maxSavings > 0 ? (float)cat.TotalPotentialSavingsBytes / maxSavings * 100f : 0f;
                var scorePercent = cat.Score;
                var scoreClass = GetScoreClass(cat.Score);

                sb.AppendLine("<tr>");
                sb.AppendLine($"<td><strong>{EscapeHtml(cat.Name)}</strong></td>");
                sb.AppendLine($"<td>{cat.IssueCount}</td>");
                sb.AppendLine($"<td>{FormatBytes(cat.TotalPotentialSavingsBytes)}</td>");
                sb.AppendLine($"<td><div class='bar-bg'><div class='bar-fill savings' style='width:{savingsPercent:F1}%'></div></div></td>");
                sb.AppendLine($"<td><div class='score-bar-bg'><div class='score-bar-fill {scoreClass}' style='width:{scorePercent}%'></div></div><span class='score-label'>{cat.Score}</span></td>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</tbody></table>");
            sb.AppendLine("</div>");
        }

        private static void DrawSeverityChart(StringBuilder sb, DiagnosticReport report)
        {
            var counts = new Dictionary<string, int>
            {
                ["Error"] = report.Issues.Count(i => i.Severity == IssueSeverity.Error),
                ["Warning"] = report.Issues.Count(i => i.Severity == IssueSeverity.Warning),
                ["Info"] = report.Issues.Count(i => i.Severity == IssueSeverity.Info)
            };
            var maxCount = counts.Values.Count > 0 ? counts.Values.Max() : 1;

            sb.AppendLine("<div class='card'>");
            sb.AppendLine("<h2>严重级别分布</h2>");
            sb.AppendLine("<div class='severity-chart'>");
            foreach (var kv in counts)
            {
                var percent = maxCount > 0 ? (float)kv.Value / maxCount * 100f : 0f;
                var severityClass = kv.Key.ToLowerInvariant();
                sb.AppendLine("<div class='severity-row'>");
                sb.AppendLine($"<div class='severity-name'>{kv.Key}</div>");
                sb.AppendLine($"<div class='bar-bg'><div class='bar-fill {severityClass}' style='width:{percent:F1}%'></div></div>");
                sb.AppendLine($"<div class='severity-count'>{kv.Value}</div>");
                sb.AppendLine("</div>");
            }
            sb.AppendLine("</div>");
            sb.AppendLine("</div>");
        }

        private static void DrawTopIssues(StringBuilder sb, DiagnosticReport report)
        {
            var top = report.Issues
                .OrderByDescending(i => i.PotentialSavingsBytes)
                .Take(10)
                .ToList();

            if (top.Count == 0) return;

            sb.AppendLine("<div class='card'>");
            sb.AppendLine("<h2>Top 10 可优化项</h2>");
            sb.AppendLine("<table class='issue-table'>");
            sb.AppendLine("<thead><tr><th>严重级别</th><th>标题</th><th>类别</th><th>预估节省</th></tr></thead>");
            sb.AppendLine("<tbody>");

            foreach (var issue in top)
            {
                sb.AppendLine("<tr>");
                sb.AppendLine($"<td><span class='tag {issue.Severity.ToString().ToLowerInvariant()}'>{issue.Severity}</span></td>");
                sb.AppendLine($"<td>{EscapeHtml(issue.Title)}</td>");
                sb.AppendLine($"<td>{EscapeHtml(issue.Category)}</td>");
                sb.AppendLine($"<td>{FormatBytes(issue.PotentialSavingsBytes)}</td>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</tbody></table>");
            sb.AppendLine("</div>");
        }

        private static void DrawHistoryTrend(StringBuilder sb, List<BuildHistoryEntry> history)
        {
            if (history == null || history.Count < 2) return;

            var width = 800;
            var height = 200;
            var padding = 40;
            var plotW = width - padding * 2;
            var plotH = height - padding * 2;

            var scores = history.Select(h => h.OverallScore).ToList();
            var minScore = Mathf.Max(0, scores.Min() - 5);
            var maxScore = Mathf.Min(100, scores.Max() + 5);
            var scoreRange = Mathf.Max(1, maxScore - minScore);

            var points = new List<string>();
            for (int i = 0; i < history.Count; i++)
            {
                var x = padding + (float)i / (history.Count - 1) * plotW;
                var y = height - padding - (history[i].OverallScore - minScore) / (float)scoreRange * plotH;
                points.Add($"{x:F1},{y:F1}");
            }

            var polyline = string.Join(" ", points);

            sb.AppendLine("<div class='card'>");
            sb.AppendLine("<h2>历史评分趋势</h2>");
            sb.AppendLine($"<svg viewBox='0 0 {width} {height}' class='trend-chart'>");
            // 网格线
            for (int i = 0; i <= 4; i++)
            {
                var y = height - padding - i / 4f * plotH;
                sb.AppendLine($"<line x1='{padding}' y1='{y}' x2='{width - padding}' y2='{y}' class='grid-line' />");
            }
            // 折线
            sb.AppendLine($"<polyline points='{polyline}' fill='none' stroke='#4caf50' stroke-width='3' stroke-linecap='round' stroke-linejoin='round' />");
            // 数据点
            for (int i = 0; i < history.Count; i++)
            {
                var x = padding + (float)i / (history.Count - 1) * plotW;
                var y = height - padding - (history[i].OverallScore - minScore) / (float)scoreRange * plotH;
                var label = "?";
                if (DateTime.TryParse(history[i].CreatedAt, out var dt))
                {
                    label = dt.ToString("MM-dd HH:mm");
                }
                sb.AppendLine($"<circle cx='{x:F1}' cy='{y:F1}' r='5' fill='#4caf50' />");
                sb.AppendLine($"<text x='{x:F1}' y='{height - 10}' text-anchor='middle' font-size='10' fill='#666'>{EscapeHtml(label)}</text>");
                sb.AppendLine($"<text x='{x:F1}' y='{y - 10:F1}' text-anchor='middle' font-size='10' fill='#333'>{history[i].OverallScore}</text>");
            }
            sb.AppendLine("</svg>");
            sb.AppendLine("</div>");
        }

        private static void DrawFullIssueList(StringBuilder sb, DiagnosticReport report)
        {
            sb.AppendLine("<div class='card'>");
            sb.AppendLine("<h2>完整问题列表</h2>");
            foreach (var issue in report.Issues)
            {
                var severityClass = issue.Severity.ToString().ToLowerInvariant();
                sb.AppendLine($"<div class='issue-item {severityClass}'>");
                sb.AppendLine($"<div class='issue-title'><span class='tag {severityClass}'>[{issue.Severity}]</span> {EscapeHtml(issue.Title)}</div>");
                sb.AppendLine($"<div class='issue-desc'>{EscapeHtml(issue.Description)}</div>");
                sb.AppendLine($"<div class='issue-meta'>资产: {EscapeHtml(issue.AssetPath)} &nbsp;|&nbsp; 类别: {EscapeHtml(issue.Category)} &nbsp;|&nbsp; 预估节省: {FormatBytes(issue.PotentialSavingsBytes)} &nbsp;|&nbsp; 可自动修复: {(issue.AutoFixable ? "✅ 是" : "否")}</div>");
                sb.AppendLine($"<div class='issue-fix'>建议: {EscapeHtml(issue.SuggestedFix)}</div>");
                sb.AppendLine("</div>");
            }
            sb.AppendLine("</div>");
        }

        private static string RenderScoreGauge(int score)
        {
            var radius = 50;
            var circumference = 2 * Mathf.PI * radius;
            var offset = circumference * (1 - score / 100f);
            var colorClass = GetScoreClass(score);
            var color = colorClass == "good" ? "#4caf50" : colorClass == "warning" ? "#ff9800" : "#f44336";

            return $"<svg viewBox='0 0 120 120' width='120' height='120'>" +
                   $"<circle cx='60' cy='60' r='{radius}' fill='none' stroke='#e0e0e0' stroke-width='12' />" +
                   $"<circle cx='60' cy='60' r='{radius}' fill='none' stroke='{color}' stroke-width='12' stroke-linecap='round' " +
                   $"stroke-dasharray='{circumference}' stroke-dashoffset='{offset}' transform='rotate(-90 60 60)' />" +
                   $"<text x='60' y='65' text-anchor='middle' font-size='26' font-weight='bold' fill='#333'>{score}</text>" +
                   $"</svg>";
        }

        private static string GetScoreClass(int score)
        {
            if (score >= 80) return "good";
            if (score >= 60) return "warning";
            return "bad";
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024f:F2} KB";
            return $"{bytes / (1024f * 1024f):F2} MB";
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

        private static string GetCss()
        {
            return @"
                :root { --error:#f44336; --warning:#ff9800; --info:#2196f3; --good:#4caf50; }
                @page { margin: 0; size: auto; }
                body { font-family: -apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,'Helvetica Neue',Arial,sans-serif; max-width:1100px; margin:0 auto; padding:20px; background:#f4f6f8; color:#333; line-height:1.5; }
                .card { background:#fff; border-radius:10px; padding:24px; margin-bottom:20px; box-shadow:0 2px 6px rgba(0,0,0,0.06); }
                h1,h2 { margin-top:0; color:#1a1a1a; }
                .header { display:flex; align-items:center; gap:28px; }
                .header-info { flex:1; }
                .meta { color:#666; font-size:0.9rem; margin:4px 0 12px; }
                .score-value { font-size:2rem; font-weight:700; }
                .score-value.good { color:var(--good); }
                .score-value.warning { color:var(--warning); }
                .score-value.bad { color:var(--error); }
                .summary-grid { display:grid; grid-template-columns:repeat(auto-fit,minmax(140px,1fr)); gap:16px; }
                .summary-item { text-align:center; padding:12px; background:#f9fafb; border-radius:8px; }
                .summary-item .value { font-size:1.6rem; font-weight:700; color:#1a1a1a; }
                .summary-item .label { font-size:0.85rem; color:#666; margin-top:4px; }
                .summary-item.error .value { color:var(--error); }
                .summary-item.warning .value { color:var(--warning); }
                .summary-item.info .value { color:var(--info); }
                table { width:100%; border-collapse:collapse; }
                th,td { padding:10px; border-bottom:1px solid #eee; text-align:left; }
                th { background:#f9fafb; font-weight:600; }
                .bar-bg { background:#e8eaed; border-radius:4px; height:18px; overflow:hidden; min-width:120px; }
                .bar-fill { height:100%; border-radius:4px; }
                .bar-fill.savings { background:linear-gradient(90deg,#42a5f5,#1976d2); }
                .bar-fill.error { background:var(--error); }
                .bar-fill.warning { background:var(--warning); }
                .bar-fill.info { background:var(--info); }
                .score-bar-bg { background:#e8eaed; border-radius:4px; height:10px; overflow:hidden; width:100px; display:inline-block; vertical-align:middle; }
                .score-bar-fill { height:100%; border-radius:4px; }
                .score-bar-fill.good { background:var(--good); }
                .score-bar-fill.warning { background:var(--warning); }
                .score-bar-fill.bad { background:var(--error); }
                .score-label { margin-left:8px; font-weight:600; }
                .severity-chart { display:flex; flex-direction:column; gap:12px; }
                .severity-row { display:flex; align-items:center; gap:12px; }
                .severity-name { width:80px; font-weight:600; }
                .severity-row .bar-bg { flex:1; }
                .severity-count { width:40px; text-align:right; font-weight:600; }
                .tag { display:inline-block; padding:3px 10px; border-radius:12px; font-size:0.75rem; color:#fff; font-weight:600; }
                .tag.error { background:var(--error); }
                .tag.warning { background:var(--warning); }
                .tag.info { background:var(--info); }
                .issue-item { padding:14px; border-radius:8px; margin-bottom:12px; background:#f9fafb; border-left:4px solid #ccc; }
                .issue-item.error { border-left-color:var(--error); }
                .issue-item.warning { border-left-color:var(--warning); }
                .issue-item.info { border-left-color:var(--info); }
                .issue-title { font-weight:600; margin-bottom:4px; }
                .issue-desc { color:#555; margin-bottom:6px; }
                .issue-meta, .issue-fix { font-size:0.85rem; color:#666; }
                .issue-fix { margin-top:4px; color:#444; }
                .trend-chart { width:100%; height:auto; max-width:800px; }
                .grid-line { stroke:#e0e0e0; stroke-width:1; }
                @media print { body { background:#fff; } .card { break-inside:avoid; box-shadow:none; border:1px solid #ddd; } }
            ";
        }
    }
}
