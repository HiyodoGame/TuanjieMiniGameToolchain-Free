using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MiniGame.PerformanceSuite.Runtime.Data;

namespace MiniGame.PerformanceSuite.Runtime.Startup
{
    /// <summary>
    /// 启动报告 HTML 导出器，内联 CSS/JS，无需外部依赖。
    /// </summary>
    public static class MPSStartupReportHtmlExporter
    {
        /// <summary>
        /// 将启动报告导出为 HTML 文件。
        /// </summary>
        public static void Export(string path, MPSStartupReport report, List<MPSStartupSuggestion> suggestions)
        {
            if (report == null) return;

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            File.WriteAllText(path, BuildHtml(report, suggestions ?? new List<MPSStartupSuggestion>()), Encoding.UTF8);
        }

        private static string BuildHtml(MPSStartupReport report, List<MPSStartupSuggestion> suggestions)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"zh-CN\">");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset=\"UTF-8\">");
            sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            sb.AppendLine("<title>MiniGame Performance Suite - Startup Report</title>");
            sb.AppendLine("<style>");
            sb.AppendLine(GetCss());
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("<div class=\"container\">");
            sb.AppendLine("<h1>启动性能报告</h1>");
            sb.AppendLine($"<p class=\"meta\">报告时间: {report.CaptureTime}</p>");
            sb.AppendLine($"<p class=\"meta\">总启动耗时: <strong>{report.TotalStartupTime:F3}s</strong></p>");

            sb.AppendLine("<h2>阶段耗时</h2>");
            sb.AppendLine("<div id=\"chart\"></div>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr><th>阶段</th><th>描述</th><th>耗时 (s)</th><th>阈值 (s)</th><th>状态</th></tr>");
            foreach (var root in report.Roots)
            {
                AppendStageRows(sb, root, 0);
            }
            sb.AppendLine("</table>");

            if (suggestions.Count > 0)
            {
                sb.AppendLine("<h2>优化建议</h2>");
                sb.AppendLine("<ul class=\"suggestions\">");
                foreach (var suggestion in suggestions)
                {
                    var cssClass = suggestion.Severity == MPSStartupSuggestionSeverity.Critical ? "critical"
                        : suggestion.Severity == MPSStartupSuggestionSeverity.Warning ? "warning" : "info";
                    sb.AppendLine($"<li class=\"{cssClass}\"><strong>[{suggestion.Severity}] {suggestion.Title}</strong><br>{suggestion.Description}</li>");
                }
                sb.AppendLine("</ul>");
            }

            sb.AppendLine("</div>");
            sb.AppendLine("<script>");
            sb.AppendLine(GetChartJs(report));
            sb.AppendLine("</script>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
            return sb.ToString();
        }

        private static void AppendStageRows(StringBuilder sb, MPSStartupStage stage, int indent)
        {
            if (stage == null) return;

            var status = !stage.IsComplete ? "running"
                : stage.IsWarning ? "warning"
                : "ok";
            var indentStyle = $"padding-left: {indent * 24}px;";

            sb.AppendLine($"<tr class=\"{status}\">");
            sb.AppendLine($"<td style=\"{indentStyle}\">{Escape(stage.Name)}</td>");
            sb.AppendLine($"<td>{Escape(stage.Description)}</td>");
            sb.AppendLine($"<td>{(stage.IsComplete ? stage.Duration.ToString("F3") : "--")}</td>");
            sb.AppendLine($"<td>{stage.WarningThreshold:F2}</td>");
            sb.AppendLine($"<td class=\"status\">{status}</td>");
            sb.AppendLine("</tr>");

            foreach (var child in stage.Children)
            {
                AppendStageRows(sb, child, indent + 1);
            }
        }

        private static string GetCss()
        {
            return @"
                body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; background: #f5f7fa; margin: 0; padding: 20px; color: #333; }
                .container { max-width: 960px; margin: 0 auto; background: #fff; padding: 32px; border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.08); }
                h1 { margin-top: 0; font-size: 28px; }
                h2 { margin-top: 32px; font-size: 20px; border-bottom: 1px solid #eee; padding-bottom: 8px; }
                .meta { color: #666; }
                table { width: 100%; border-collapse: collapse; margin-top: 16px; }
                th, td { text-align: left; padding: 10px 12px; border-bottom: 1px solid #eee; }
                th { background: #f8f9fa; font-weight: 600; }
                tr.warning { background: #fff3cd; }
                tr.ok { background: #d4edda; }
                tr.running { background: #fff9c4; }
                td.status { font-weight: 600; text-transform: capitalize; }
                .suggestions { list-style: none; padding: 0; }
                .suggestions li { padding: 12px 16px; margin-bottom: 10px; border-radius: 6px; }
                .suggestions .critical { background: #f8d7da; border-left: 4px solid #dc3545; }
                .suggestions .warning { background: #fff3cd; border-left: 4px solid #ffc107; }
                .suggestions .info { background: #d1ecf1; border-left: 4px solid #17a2b8; }
                #chart { margin: 20px 0; }
                .bar-row { display: flex; align-items: center; margin-bottom: 8px; }
                .bar-label { width: 160px; font-size: 13px; color: #555; }
                .bar-wrap { flex: 1; background: #e9ecef; border-radius: 4px; height: 22px; overflow: hidden; }
                .bar-fill { height: 100%; background: #4dabf7; border-radius: 4px; transition: width 0.6s ease; }
                .bar-value { width: 70px; text-align: right; font-size: 13px; color: #555; padding-left: 10px; }
            ";
        }

        private static string GetChartJs(MPSStartupReport report)
        {
            var sb = new StringBuilder();
            sb.AppendLine("(function(){");
            sb.AppendLine("var chart = document.getElementById('chart');");
            sb.AppendLine("var data = [");

            double maxDuration = 0.001;
            foreach (var root in report.Roots)
            {
                if (root.IsComplete && root.Duration > maxDuration) maxDuration = root.Duration;
            }

            foreach (var root in report.Roots)
            {
                if (!root.IsComplete) continue;
                var width = (root.Duration / maxDuration * 100).ToString("F1");
                sb.AppendLine($"{{name:'{EscapeJs(root.Name)}',value:{root.Duration:F3},width:{width}}},");
            }

            sb.AppendLine("];");
            sb.AppendLine("data.forEach(function(item){");
            sb.AppendLine("var row = document.createElement('div'); row.className='bar-row';");
            sb.AppendLine("row.innerHTML = '<div class=\"bar-label\">' + item.name + '</div>' +");
            sb.AppendLine("'<div class=\"bar-wrap\"><div class=\"bar-fill\" style=\"width:0%\"></div></div>' +");
            sb.AppendLine("'<div class=\"bar-value\">' + item.value.toFixed(3) + 's</div>';");
            sb.AppendLine("chart.appendChild(row);");
            sb.AppendLine("setTimeout(function(){ row.querySelector('.bar-fill').style.width = item.width + '%'; }, 50);");
            sb.AppendLine("});");
            sb.AppendLine("})();");
            return sb.ToString();
        }

        private static string Escape(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }

        private static string EscapeJs(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return text.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\n", "\\n").Replace("\r", "\\r");
        }
    }
}
