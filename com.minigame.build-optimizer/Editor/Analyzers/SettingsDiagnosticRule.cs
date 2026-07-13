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
                    AutoFixable = true
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
                    AutoFixable = true
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
                    AutoFixable = true
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
                    AutoFixable = true
                });
            }

            return issues;
        }
    }
}
