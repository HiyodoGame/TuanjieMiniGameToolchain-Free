using System.Collections.Generic;
using MiniGame.Core.Editor.Analyzers;
using UnityEditor;
using UnityEngine;

namespace MiniGame.BuildOptimizer.Editor.Analyzers
{
    /// <summary>
    /// PlayerSettings / Build Settings 合规性诊断规则。
    /// </summary>
    public class SettingsDiagnosticRule : DiagnosticRule
    {
        public SettingsDiagnosticRule()
        {
            RuleId = "SETTINGS_DIAGNOSTIC";
            Category = "Settings";
            DefaultSeverity = IssueSeverity.Error;
            DisplayName = "构建设置诊断";
        }

        public override List<DiagnosticIssue> Evaluate(DiagnosticContext context)
        {
            var issues = new List<DiagnosticIssue>();
            var targetGroup = BuildTargetGroup.WebGL;

            // 目标平台检查：必须在 WebGL 平台下才有意义
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
            {
                issues.Add(new DiagnosticIssue
                {
                    RuleId = RuleId,
                    Category = Category,
                    Severity = IssueSeverity.Info,
                    Title = "当前激活平台不是 WebGL",
                    Description = "构建设置诊断仅针对 WebGL（微信小游戏）平台。切换到 WebGL 平台后可获得更准确的建议。",
                    AssetPath = "ProjectSettings/ProjectSettings.asset",
                    SuggestedFix = "切换 Build Target 到 WebGL。",
                    PotentialSavingsBytes = 0,
                    AutoFixable = false
                });
            }

            // IL2CPP 检查
            var scriptingBackend = PlayerSettings.GetScriptingBackend(targetGroup);
            if (scriptingBackend != ScriptingImplementation.IL2CPP)
            {
                issues.Add(new DiagnosticIssue
                {
                    RuleId = RuleId,
                    Category = Category,
                    Severity = IssueSeverity.Error,
                    Title = "未使用 IL2CPP",
                    Description = $"当前脚本后端为 {scriptingBackend}，微信小游戏建议使用 IL2CPP 以获得更优性能和更小包体。",
                    AssetPath = "ProjectSettings/ProjectSettings.asset",
                    SuggestedFix = "将 WebGL 平台的 Scripting Backend 设置为 IL2CPP。",
                    PotentialSavingsBytes = 5 * 1024 * 1024, // 粗略估算
                    AutoFixable = true,
                    FixKey = "Settings.IL2CPP"
                });
            }

            // Managed Stripping Level 检查
            var strippingLevel = PlayerSettings.GetManagedStrippingLevel(targetGroup);
            if (strippingLevel != ManagedStrippingLevel.High && strippingLevel != ManagedStrippingLevel.Extreme)
            {
                issues.Add(new DiagnosticIssue
                {
                    RuleId = RuleId,
                    Category = Category,
                    Severity = IssueSeverity.Warning,
                    Title = "Managed Stripping Level 偏低",
                    Description = $"当前 Stripping Level 为 {strippingLevel}，建议设置为 High 或 Extreme 以减少 WASM 体积。",
                    AssetPath = "ProjectSettings/ProjectSettings.asset",
                    SuggestedFix = "将 Managed Stripping Level 设置为 High。",
                    PotentialSavingsBytes = 3 * 1024 * 1024,
                    AutoFixable = true,
                    FixKey = "Settings.Stripping"
                });
            }

            // WebGL 压缩格式检查
            var compression = PlayerSettings.WebGL.compressionFormat;
            if (compression != WebGLCompressionFormat.Brotli)
            {
                issues.Add(new DiagnosticIssue
                {
                    RuleId = RuleId,
                    Category = Category,
                    Severity = IssueSeverity.Warning,
                    Title = "WebGL 压缩格式非 Brotli",
                    Description = $"当前压缩格式为 {compression}，Brotli 通常具有更高的压缩率。",
                    AssetPath = "ProjectSettings/ProjectSettings.asset",
                    SuggestedFix = "将 WebGL Compression Format 设置为 Brotli。",
                    PotentialSavingsBytes = 2 * 1024 * 1024,
                    AutoFixable = true,
                    FixKey = "Settings.Brotli"
                });
            }

            // 异常支持检查
            var exceptionSupport = PlayerSettings.WebGL.exceptionSupport;
            if (exceptionSupport != WebGLExceptionSupport.None)
            {
                issues.Add(new DiagnosticIssue
                {
                    RuleId = RuleId,
                    Category = Category,
                    Severity = IssueSeverity.Info,
                    Title = "WebGL 异常支持未关闭",
                    Description = $"当前 Exception Support 为 {exceptionSupport}，发布版建议关闭以减少包体。",
                    AssetPath = "ProjectSettings/ProjectSettings.asset",
                    SuggestedFix = "将 WebGL Exception Support 设置为 None。",
                    PotentialSavingsBytes = 1 * 1024 * 1024,
                    AutoFixable = true,
                    FixKey = "Settings.ExceptionNone"
                });
            }

            // 内存大小检查
            var memorySize = PlayerSettings.WebGL.memorySize;
            if (memorySize > 512)
            {
                issues.Add(new DiagnosticIssue
                {
                    RuleId = RuleId,
                    Category = Category,
                    Severity = IssueSeverity.Warning,
                    Title = "WebGL Memory Size 过大",
                    Description = $"当前 WebGL Memory Size 为 {memorySize} MB，微信小游戏建议根据目标机型设置为 256 MB 或更低。",
                    AssetPath = "ProjectSettings/ProjectSettings.asset",
                    SuggestedFix = "将 WebGL Memory Size 设置为 256 MB。",
                    PotentialSavingsBytes = (memorySize - 256) * 1024 * 1024,
                    AutoFixable = true,
                    FixKey = "Settings.Memory256",
                    FixData = "256"
                });
            }

            // Data Caching 检查
            var dataCaching = PlayerSettings.WebGL.dataCaching;
            if (!dataCaching)
            {
                issues.Add(new DiagnosticIssue
                {
                    RuleId = RuleId,
                    Category = Category,
                    Severity = IssueSeverity.Warning,
                    Title = "WebGL Data Caching 未启用",
                    Description = "未启用 Data Caching，会导致玩家每次启动都重新下载资源，影响启动速度。",
                    AssetPath = "ProjectSettings/ProjectSettings.asset",
                    SuggestedFix = "启用 WebGL Data Caching。",
                    PotentialSavingsBytes = 0,
                    AutoFixable = true,
                    FixKey = "Settings.DataCaching"
                });
            }

            // Name Files As Hashes 检查
            if (!PlayerSettings.WebGL.nameFilesAsHashes)
            {
                issues.Add(new DiagnosticIssue
                {
                    RuleId = RuleId,
                    Category = Category,
                    Severity = IssueSeverity.Info,
                    Title = "WebGL Name Files As Hashes 未启用",
                    Description = "未启用 nameFilesAsHashes，资源更新时 CDN 缓存可能导致玩家加载旧版本。",
                    AssetPath = "ProjectSettings/ProjectSettings.asset",
                    SuggestedFix = "启用 WebGL Name Files As Hashes。",
                    PotentialSavingsBytes = 0,
                    AutoFixable = true,
                    FixKey = "Settings.NameFilesAsHashes"
                });
            }

            // Debug Symbol Mode 检查
            if (PlayerSettings.WebGL.debugSymbolMode != WebGLDebugSymbolMode.Off)
            {
                issues.Add(new DiagnosticIssue
                {
                    RuleId = RuleId,
                    Category = Category,
                    Severity = IssueSeverity.Warning,
                    Title = "WebGL Debug Symbol Mode 未关闭",
                    Description = $"当前 Debug Symbol Mode 为 {PlayerSettings.WebGL.debugSymbolMode}，发布版建议关闭以减小包体。",
                    AssetPath = "ProjectSettings/ProjectSettings.asset",
                    SuggestedFix = "将 WebGL Debug Symbol Mode 设置为 Off。",
                    PotentialSavingsBytes = 1 * 1024 * 1024,
                    AutoFixable = true,
                    FixKey = "Settings.DebugSymbolOff"
                });
            }

            // Strip Engine Code 检查
            if (!PlayerSettings.stripEngineCode)
            {
                issues.Add(new DiagnosticIssue
                {
                    RuleId = RuleId,
                    Category = Category,
                    Severity = IssueSeverity.Warning,
                    Title = "Strip Engine Code 未启用",
                    Description = "未启用 Strip Engine Code，会保留未使用的引擎代码，增加 WASM 体积。",
                    AssetPath = "ProjectSettings/ProjectSettings.asset",
                    SuggestedFix = "启用 PlayerSettings.stripEngineCode。",
                    PotentialSavingsBytes = 2 * 1024 * 1024,
                    AutoFixable = true,
                    FixKey = "Settings.StripEngineCode"
                });
            }

            // wasmCodeSplit 检查（微信小游戏特有，反射读取）
            var wasmCodeSplit = GetWasmCodeSplit();
            if (wasmCodeSplit.HasValue && !wasmCodeSplit.Value)
            {
                issues.Add(new DiagnosticIssue
                {
                    RuleId = RuleId,
                    Category = Category,
                    Severity = IssueSeverity.Warning,
                    Title = "WASM Code Splitting 未启用",
                    Description = "未启用 wasmCodeSplit，WASM 代码包会偏大，影响首包体积和启动速度。",
                    AssetPath = "ProjectSettings/ProjectSettings.asset",
                    SuggestedFix = "在微信小游戏转换插件设置中启用 wasmCodeSplit。",
                    PotentialSavingsBytes = 5 * 1024 * 1024,
                    AutoFixable = true,
                    FixKey = "Settings.WasmCodeSplit"
                });
            }

            return issues;
        }

        private static bool? GetWasmCodeSplit()
        {
            try
            {
                var webglSettingsType = typeof(PlayerSettings.WebGL);
                var prop = webglSettingsType.GetProperty("wasmCodeSplit",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                if (prop != null)
                {
                    return (bool)prop.GetValue(null);
                }
            }
            catch
            {
                // 属性不存在或无法读取时忽略
            }
            return null;
        }
    }
}
