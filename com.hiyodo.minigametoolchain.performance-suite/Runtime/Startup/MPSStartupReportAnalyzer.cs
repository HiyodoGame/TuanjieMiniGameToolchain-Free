using System;
using System.Collections.Generic;
using MiniGame.PerformanceSuite.Runtime.Data;

namespace MiniGame.PerformanceSuite.Runtime.Startup
{
    /// <summary>
    /// 启动报告分析器：基于各阶段耗时输出优化建议。
    /// </summary>
    public static class MPSStartupReportAnalyzer
    {
        /// <summary>
        /// 分析启动报告并返回优化建议列表。
        /// </summary>
        public static List<MPSStartupSuggestion> Analyze(MPSStartupReport report)
        {
            var suggestions = new List<MPSStartupSuggestion>();
            if (report == null || report.Roots == null) return suggestions;

            AddOverallSuggestion(report, suggestions);

            foreach (var root in report.Roots)
            {
                AnalyzeStage(root, suggestions);
            }

            suggestions.Sort((a, b) => b.Severity.CompareTo(a.Severity));
            return suggestions;
        }

        private static void AddOverallSuggestion(MPSStartupReport report, List<MPSStartupSuggestion> suggestions)
        {
            if (report.TotalStartupTime > 10.0)
            {
                suggestions.Add(new MPSStartupSuggestion(
                    MPSStartupSuggestionSeverity.Critical,
                    "启动总耗时过长",
                    $"总启动时间为 {report.TotalStartupTime:F2}s，远超小游戏用户可接受范围（建议 <5s）。请结合下方各阶段耗时逐项优化。"));
            }
            else if (report.TotalStartupTime > 5.0)
            {
                suggestions.Add(new MPSStartupSuggestion(
                    MPSStartupSuggestionSeverity.Warning,
                    "启动总耗时偏长",
                    $"总启动时间为 {report.TotalStartupTime:F2}s，建议优化至 5s 以内以提升留存。"));
            }
        }

        private static void AnalyzeStage(MPSStartupStage stage, List<MPSStartupSuggestion> suggestions)
        {
            if (stage == null || !stage.IsComplete) return;

            switch (stage.Name)
            {
                case "WasmCompile":
                case "WASMCompile":
                case "WasmDownload":
                    if (stage.Duration > 3.0)
                    {
                        suggestions.Add(new MPSStartupSuggestion(
                            MPSStartupSuggestionSeverity.Critical,
                            "WASM 编译/下载耗时过长",
                            "建议启用 wasmCodeSplit（微信小游戏）并开启 Brotli 压缩，减少首包 WASM 体积。"));
                    }
                    break;

                case "FirstSceneLoad":
                case "SceneLoad":
                    if (stage.Duration > 2.0)
                    {
                        suggestions.Add(new MPSStartupSuggestion(
                            MPSStartupSuggestionSeverity.Warning,
                            "首场景加载耗时过长",
                            "建议启用 AutoStreaming、拆分首场景资源，或将同步加载改为启动阶段异步预加载。"));
                    }
                    break;

                case "FirstPackageDownload":
                case "GameJsDownload":
                    if (stage.Duration > 1.0)
                    {
                        suggestions.Add(new MPSStartupSuggestion(
                            MPSStartupSuggestionSeverity.Warning,
                            "首包下载耗时过长",
                            "建议启用 Brotli/Gzip 压缩、减少首包体积，或拆分非必要资源为远程包。"));
                    }
                    break;

                case "ShaderWarmUp":
                    if (stage.Duration > 2.0)
                    {
                        suggestions.Add(new MPSStartupSuggestion(
                            MPSStartupSuggestionSeverity.Warning,
                            "Shader WarmUp 耗时过长",
                            "建议剔除冗余 Shader 变体、使用 ShaderVariantCollection 分帧预热，或降低 WarmUp 优先级。"));
                    }
                    break;

                case "AssetWarmUp":
                    if (stage.Duration > 3.0)
                    {
                        suggestions.Add(new MPSStartupSuggestion(
                            MPSStartupSuggestionSeverity.Warning,
                            "资源 WarmUp 耗时过长",
                            "建议将首包资源预热改为异步预加载，避免阻塞主线程。"));
                    }
                    break;

                case "WeChatApiInit":
                    if (stage.Duration > 1.0)
                    {
                        suggestions.Add(new MPSStartupSuggestion(
                            MPSStartupSuggestionSeverity.Info,
                            "微信 API 初始化耗时偏长",
                            "检查是否在初始化阶段同步等待微信 API，考虑延后非必要 API 调用。"));
                    }
                    break;
            }

            if (stage.IsWarning && stage.Children.Count == 0)
            {
                suggestions.Add(new MPSStartupSuggestion(
                    MPSStartupSuggestionSeverity.Warning,
                    $"阶段 {stage.Name} 超过阈值",
                    $"{stage.Name} 耗时 {stage.Duration:F2}s，超过阈值 {stage.WarningThreshold:F2}s。"));
            }

            foreach (var child in stage.Children)
            {
                AnalyzeStage(child, suggestions);
            }
        }
    }
}
