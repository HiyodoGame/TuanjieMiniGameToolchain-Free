using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MiniGame.PerformanceSuite.Runtime.Data
{
    /// <summary>
    /// 内存分析报告中单个资源条目的信息。
    /// </summary>
    [Serializable]
    public class MPSMemoryAnalysisAssetEntry
    {
        public string Type;
        public string Name;
        public long SizeBytes;
    }

    /// <summary>
    /// 内存分析报告。
    /// </summary>
    [Serializable]
    public class MPSMemoryAnalysisReport
    {
        public string RunTime;
        public string DeviceIdentifier;
        public long DeviceLimitBytes;
        public long EstimatedTotalBytes;
        public long UnityHeapBytes;
        public long MonoHeapBytes;
        public long JsHeapBytes;
        public long GfxDriverBytes;
        public long WasmCompileEstimateBytes;
        public List<MPSMemoryAnalysisAssetEntry> TopAssets = new List<MPSMemoryAnalysisAssetEntry>();
        public string WarningLevel; // None / Yellow / Red
        public List<string> Suggestions = new List<string>();

        public static MPSMemoryAnalysisReport Capture(
            string deviceIdentifier,
            long deviceLimitBytes,
            long unityHeapBytes,
            long monoHeapBytes,
            long jsHeapBytes,
            long gfxDriverBytes,
            long wasmCompileEstimateBytes,
            IEnumerable<MPSMemoryAnalysisAssetEntry> topAssets)
        {
            var report = new MPSMemoryAnalysisReport
            {
                RunTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                DeviceIdentifier = deviceIdentifier,
                DeviceLimitBytes = deviceLimitBytes,
                UnityHeapBytes = unityHeapBytes,
                MonoHeapBytes = monoHeapBytes,
                JsHeapBytes = jsHeapBytes,
                GfxDriverBytes = gfxDriverBytes,
                WasmCompileEstimateBytes = wasmCompileEstimateBytes,
                EstimatedTotalBytes = unityHeapBytes + monoHeapBytes + gfxDriverBytes + wasmCompileEstimateBytes,
                TopAssets = topAssets?.ToList() ?? new List<MPSMemoryAnalysisAssetEntry>()
            };

            var ratio = deviceLimitBytes > 0 ? report.EstimatedTotalBytes / (float)deviceLimitBytes : 0f;
            if (ratio >= 0.95f)
            {
                report.WarningLevel = "Red";
                report.Suggestions.Add("当前预估内存已接近/超过 iOS 上限，极易在 2GB 机型上闪退。");
                report.Suggestions.Add("建议优先压缩纹理、减少 Shader 变体、降低 Audio 质量。");
            }
            else if (ratio >= 0.8f)
            {
                report.WarningLevel = "Yellow";
                report.Suggestions.Add("当前预估内存接近 iOS 建议上限，建议关注资源加载策略。");
                report.Suggestions.Add("考虑启用 AutoStreaming 或 Addressables 远程分包。");
            }
            else
            {
                report.WarningLevel = "None";
                report.Suggestions.Add("当前内存处于安全区间。");
            }

            return report;
        }

        public void SaveMarkdown(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            var sb = new StringBuilder();
            sb.AppendLine($"# 内存分析报告");
            sb.AppendLine();
            sb.AppendLine($"- **分析时间**: {RunTime}");
            sb.AppendLine($"- **目标设备**: {DeviceIdentifier}");
            sb.AppendLine($"- **设备内存上限**: {FormatBytes(DeviceLimitBytes)}");
            sb.AppendLine($"- **预估总内存**: {FormatBytes(EstimatedTotalBytes)} ({EstimatedTotalBytes * 100f / DeviceLimitBytes:F1}% of limit)");
            sb.AppendLine($"- **Unity Heap**: {FormatBytes(UnityHeapBytes)}");
            sb.AppendLine($"- **Mono Heap**: {FormatBytes(MonoHeapBytes)}");
            sb.AppendLine($"- **JS Heap**: {FormatBytes(JsHeapBytes)}");
            sb.AppendLine($"- **GFX Driver**: {FormatBytes(GfxDriverBytes)}");
            sb.AppendLine($"- **WASM 编译估算**: {FormatBytes(WasmCompileEstimateBytes)}");
            sb.AppendLine($"- **预警等级**: {WarningLevel}");
            sb.AppendLine();

            if (Suggestions.Count > 0)
            {
                sb.AppendLine("## 优化建议");
                foreach (var suggestion in Suggestions)
                {
                    sb.AppendLine($"- {suggestion}");
                }
                sb.AppendLine();
            }

            sb.AppendLine("## Top 内存资源");
            sb.AppendLine();
            sb.AppendLine("| 类型 | 名称 | 大小 |");
            sb.AppendLine("|------|------|------|");
            foreach (var entry in TopAssets.Take(50))
            {
                sb.AppendLine($"| {entry.Type} | {entry.Name} | {FormatBytes(entry.SizeBytes)} |");
            }

            File.WriteAllText(path, sb.ToString());
        }

        public static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            return $"{bytes / (1024.0 * 1024):F1} MB";
        }
    }
}
