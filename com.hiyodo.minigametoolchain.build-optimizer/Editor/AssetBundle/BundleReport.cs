using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MiniGame.BuildOptimizer.Editor.AssetBundle
{
    /// <summary>
    /// 分包分析报告，支持 JSON / Markdown 导出。
    /// </summary>
    [Serializable]
    public class BundleReport
    {
        public string GeneratedAt;
        public string ProjectName;
        public string UnityVersion;
        public BundleStrategy Strategy;

        public static BundleReport Capture(BundleStrategy strategy)
        {
            return new BundleReport
            {
                GeneratedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                ProjectName = Application.productName,
                UnityVersion = Application.unityVersion,
                Strategy = strategy
            };
        }

        public void SaveJson(string path)
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(path, JsonUtility.ToJson(this, true), Encoding.UTF8);
        }

        public void SaveMarkdown(string path)
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            var sb = new StringBuilder();
            sb.AppendLine($"# Build Optimizer — 自动分包报告");
            sb.AppendLine();
            sb.AppendLine($"- **项目**: {ProjectName}");
            sb.AppendLine($"- **引擎版本**: {UnityVersion}");
            sb.AppendLine($"- **生成时间**: {GeneratedAt}");
            sb.AppendLine();

            sb.AppendLine("## 摘要");
            sb.AppendLine();
            sb.AppendLine($"| 指标 | 数值 |");
            sb.AppendLine($"|------|------|");
            sb.AppendLine($"| 分组总数 | {Strategy.Groups.Count} |");
            sb.AppendLine($"| 首包估算 | {FormatBytes(Strategy.EstimatedFirstPackageSizeBytes)} |");
            sb.AppendLine($"| 远程包估算 | {FormatBytes(Strategy.EstimatedRemotePackageSizeBytes)} |");
            sb.AppendLine($"| 预下载估算 | {FormatBytes(Strategy.EstimatedPreloadSizeBytes)} |");
            sb.AppendLine();

            if (Strategy.Warnings.Count > 0)
            {
                sb.AppendLine("## 警告");
                sb.AppendLine();
                foreach (var warning in Strategy.Warnings)
                {
                    sb.AppendLine($"- ⚠️ {EscapeMarkdown(warning)}");
                }
                sb.AppendLine();
            }

            sb.AppendLine("## 分组详情");
            sb.AppendLine();
            sb.AppendLine($"| 分组 | 类型 | 资产数 | 估算大小 | 优先级 |");
            sb.AppendLine($"|------|------|--------|----------|--------|");
            foreach (var group in Strategy.Groups)
            {
                sb.AppendLine($"| {EscapeMarkdown(group.GroupName)} | {group.Type} | {group.AssetGuids.Count} | {FormatBytes(group.EstimatedSizeBytes)} | {group.DownloadPriority} |");
            }
            sb.AppendLine();

            sb.AppendLine("## 各组资产清单");
            sb.AppendLine();
            foreach (var group in Strategy.Groups)
            {
                sb.AppendLine($"### {EscapeMarkdown(group.GroupName)} ({group.Type})");
                sb.AppendLine();
                if (group.AssetGuids.Count == 0)
                {
                    sb.AppendLine("_无资产_");
                }
                else
                {
                    sb.AppendLine($"| 资产路径 | GUID | 大小 |");
                    sb.AppendLine($"|----------|------|------|");
                    foreach (var guid in group.AssetGuids.Take(100))
                    {
                        var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                        var size = CalculateSize(guid);
                        sb.AppendLine($"| {EscapeMarkdown(assetPath)} | `{guid.Substring(0, 8)}...` | {FormatBytes(size)} |");
                    }
                    if (group.AssetGuids.Count > 100)
                    {
                        sb.AppendLine($"| ... 共 {group.AssetGuids.Count} 项 | | |");
                    }
                }
                sb.AppendLine();
            }

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024f:F2} KB";
            return $"{bytes / (1024f * 1024f):F2} MB";
        }

        private static string EscapeMarkdown(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return text.Replace("|", "\\|").Replace("\n", " ").Replace("\r", " ");
        }

        private static long CalculateSize(string guid)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) return 0;
            var fileInfo = new FileInfo(path);
            return fileInfo.Exists ? fileInfo.Length : 0;
        }
    }
}
