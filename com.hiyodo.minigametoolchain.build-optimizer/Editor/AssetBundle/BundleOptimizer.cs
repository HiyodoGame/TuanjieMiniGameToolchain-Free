using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MiniGame.BuildOptimizer.Editor.AssetBundle
{
    /// <summary>
    /// Build Optimizer Pro — 智能分包优化入口。
    /// </summary>
    public static class BundleOptimizer
    {
        /// <summary>
        /// 分析项目并返回分包策略（不修改项目）。
        /// </summary>
        public static BundleStrategy Analyze(IProgress<string> progress = null)
        {
            progress?.Report("[BundleOptimizer] 开始分析项目分包策略...");

            var analyzer = new AssetBundleAnalyzer
            {
                FirstPackageMaxSizeBytes = 4L * 1024 * 1024,
                PreloadMaxSizeBytes = 5L * 1024 * 1024,
                PreloadMaxFileCount = 10
            };

            var strategy = analyzer.AnalyzeProject();

            progress?.Report($"[BundleOptimizer] 分析完成：{strategy.Groups.Count} 个分组，首包 {FormatBytes(strategy.EstimatedFirstPackageSizeBytes)}。");
            return strategy;
        }

        /// <summary>
        /// 分析项目并导出 JSON/Markdown 报告。
        /// </summary>
        public static BundleReport AnalyzeAndExport(string outputDirectory, IProgress<string> progress = null)
        {
            if (string.IsNullOrWhiteSpace(outputDirectory))
                throw new ArgumentException("输出目录不能为空", nameof(outputDirectory));

            Directory.CreateDirectory(outputDirectory);

            var strategy = Analyze(progress);
            var report = BundleReport.Capture(strategy);

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var jsonPath = Path.Combine(outputDirectory, $"BundleReport_{timestamp}.json");
            var mdPath = Path.Combine(outputDirectory, $"BundleReport_{timestamp}.md");

            report.SaveJson(jsonPath);
            report.SaveMarkdown(mdPath);

            progress?.Report($"[BundleOptimizer] 报告已保存: {jsonPath}");
            return report;
        }

        /// <summary>
        /// 将分析结果写入经典 AssetBundle 名称（AssetImporter.assetBundleName）。
        /// </summary>
        public static void ApplyClassicAssetBundleNames(BundleStrategy strategy, IProgress<string> progress = null)
        {
            if (strategy?.Groups == null) return;

            progress?.Report("[BundleOptimizer] 正在应用经典 AssetBundle 名称...");

            foreach (var group in strategy.Groups)
            {
                var bundleName = group.Type == BundleGroupType.FirstPackage
                    ? "firstpackage"
                    : group.GroupName.ToLowerInvariant().Replace(' ', '_');

                foreach (var guid in group.AssetGuids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (string.IsNullOrEmpty(path)) continue;
                    if (path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase)) continue;

                    var importer = AssetImporter.GetAtPath(path);
                    if (importer == null) continue;

                    importer.assetBundleName = bundleName;
                }
            }

            AssetDatabase.SaveAssets();
            progress?.Report("[BundleOptimizer] 经典 AssetBundle 名称应用完成。");
        }

        /// <summary>
        /// 根据策略生成 Addressables 分组配置（未安装 Addressables 时抛出 InvalidOperationException）。
        /// </summary>
        public static void ApplyAddressablesConfig(BundleStrategy strategy, string remoteLoadPath, IProgress<string> progress = null)
        {
            if (!AddressablesConfigGenerator.IsAddressablesAvailable)
            {
                throw new InvalidOperationException("Addressables package 未安装，无法生成分组配置。");
            }

            progress?.Report("[BundleOptimizer] 正在生成 Addressables 配置...");
            AddressablesConfigGenerator.GenerateAddressablesConfig(strategy, remoteLoadPath);
            progress?.Report("[BundleOptimizer] Addressables 配置生成完成。");
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024f:F2} KB";
            return $"{bytes / (1024f * 1024f):F2} MB";
        }
    }
}
