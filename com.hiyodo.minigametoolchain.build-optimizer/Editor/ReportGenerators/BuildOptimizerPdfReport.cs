using System;
using System.IO;
using System.Linq;
using MiniGame.Core.Editor.ReportGenerators;
using UnityEditor;
using UnityEngine;

namespace MiniGame.BuildOptimizer.Editor.ReportGenerators
{
    /// <summary>
    /// Build Optimizer PDF 报告导出器。
    /// 通过系统已安装的 Chromium/Edge 浏览器将可视化 HTML 报告打印为 PDF。
    /// </summary>
    public static class BuildOptimizerPdfReport
    {
        private const int PrintTimeoutMs = 30000;

        /// <summary>
        /// 将诊断报告导出为 PDF。
        /// </summary>
        /// <returns>是否导出成功。</returns>
        public static bool Export(DiagnosticReport report, string outputPath)
        {
            if (report == null)
            {
                Debug.LogWarning("[BuildOptimizer] 报告为空，无法导出 PDF。");
                return false;
            }

            var browser = FindBrowserExecutable();
            if (string.IsNullOrEmpty(browser))
            {
                var fallbackHtml = Path.ChangeExtension(outputPath, ".html");
                BuildOptimizerHtmlReport.Export(report, fallbackHtml);
                EditorUtility.DisplayDialog(
                    "无法导出 PDF",
                    $"未找到可用的 Chromium/Edge 浏览器，已自动导出可视化 HTML 报告：\n\n{fallbackHtml}\n\n请用浏览器打开后打印为 PDF。",
                    "确定");
                Application.OpenURL(fallbackHtml);
                return false;
            }

            var tempDir = Path.Combine("Temp", "MiniGameToolchain", "PdfExport");
            Directory.CreateDirectory(tempDir);
            var tempHtml = Path.Combine(tempDir, $"report_{DateTime.Now:yyyyMMdd_HHmmss}.html");
            var tempPdf = Path.ChangeExtension(tempHtml, ".pdf");

            try
            {
                BuildOptimizerHtmlReport.Export(report, tempHtml);

                var args = BuildPrintArguments(tempHtml, tempPdf);
                var psi = new System.Diagnostics.ProcessStartInfo(browser, args)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                Debug.Log($"[BuildOptimizer] 正在调用浏览器生成 PDF: {browser}");
                using (var process = System.Diagnostics.Process.Start(psi))
                {
                    if (process == null)
                    {
                        Debug.LogError("[BuildOptimizer] 启动浏览器失败。");
                        return false;
                    }

                    var stdout = process.StandardOutput.ReadToEnd();
                    var stderr = process.StandardError.ReadToEnd();

                    process.WaitForExit(PrintTimeoutMs);
                    if (!process.HasExited)
                    {
                        Debug.LogWarning("[BuildOptimizer] 浏览器打印超时，尝试强制结束。");
                        try { process.Kill(); } catch { }
                    }

                    if (!string.IsNullOrWhiteSpace(stderr))
                    {
                        Debug.LogWarning($"[BuildOptimizer] 浏览器输出: {stderr}");
                    }

                    if (process.ExitCode != 0)
                    {
                        Debug.LogError($"[BuildOptimizer] 浏览器退出码非零: {process.ExitCode}\n{stdout}\n{stderr}");
                    }
                }

                if (!File.Exists(tempPdf))
                {
                    Debug.LogError("[BuildOptimizer] 浏览器未生成 PDF 文件。");
                    return false;
                }

                File.Copy(tempPdf, outputPath, true);
                Debug.Log($"[BuildOptimizer] PDF 报告已导出: {outputPath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BuildOptimizer] 导出 PDF 失败: {ex}");
                return false;
            }
            finally
            {
                SafeDelete(tempHtml);
                SafeDelete(tempPdf);
            }
        }

        private static string BuildPrintArguments(string htmlPath, string pdfPath)
        {
            var htmlUri = new Uri(Path.GetFullPath(htmlPath)).AbsoluteUri;
            var pdfArg = $"--print-to-pdf=\"{pdfPath}\"";
            return $"--headless --disable-gpu --run-all-compositor-stages-before-draw --print-to-pdf-no-header {pdfArg} \"{htmlUri}\"";
        }

        private static string FindBrowserExecutable()
        {
            var candidates = new[]
            {
                @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
                @"C:\Program Files\Microsoft\Edge\Application\msedge.exe",
                @"C:\Program Files\Google\Chrome\Application\chrome.exe",
                @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
            };

            foreach (var path in candidates)
            {
                if (File.Exists(path))
                    return path;
            }

            // 尝试通过 where 命令查找
            var wherePath = FindViaWhere("msedge.exe") ?? FindViaWhere("chrome.exe");
            return wherePath;
        }

        private static string FindViaWhere(string exeName)
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo("where", exeName)
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using (var process = System.Diagnostics.Process.Start(psi))
                {
                    if (process == null) return null;
                    var output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit(5000);
                    var line = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                    if (!string.IsNullOrEmpty(line) && File.Exists(line))
                        return line;
                }
            }
            catch
            {
                // 忽略
            }
            return null;
        }

        private static void SafeDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
                // 忽略临时文件清理失败
            }
        }
    }
}
