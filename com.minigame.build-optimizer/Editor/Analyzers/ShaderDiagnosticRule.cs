using System;
using System.Collections.Generic;
using System.Linq;
using MiniGame.Core.Editor.Analyzers;
using UnityEditor;
using UnityEngine;

namespace MiniGame.BuildOptimizer.Editor.Analyzers
{
    /// <summary>
    /// Shader 诊断规则：检测变体数量爆炸、冗余 ShaderVariantCollection 变体。
    /// </summary>
    public class ShaderDiagnosticRule : DiagnosticRule
    {
        public int VariantCountThreshold = 1000;
        public int AlwaysIncludedVariantThreshold = 100;

        public ShaderDiagnosticRule()
        {
            RuleId = "SHADER_DIAGNOSTIC";
            Category = "Shader";
            DefaultSeverity = IssueSeverity.Warning;
            DisplayName = "Shader 诊断";
        }

        public override List<DiagnosticIssue> Evaluate(DiagnosticContext context)
        {
            var issues = new List<DiagnosticIssue>();

            // 扫描 Shader 文件
            var shaderGuids = AssetDatabase.FindAssets("t:Shader", new[] { "Assets" });
            foreach (var guid in shaderGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var shader = AssetDatabase.LoadAssetAtPath<UnityEngine.Shader>(path);
                if (shader == null) continue;

                int variantCount = 0;
                try
                {
                    variantCount = GetShaderVariantCount(shader);
                }
                catch
                {
                    // 某些内部 Shader 可能无法读取，忽略
                }

                if (variantCount > VariantCountThreshold)
                {
                    issues.Add(new DiagnosticIssue
                    {
                        RuleId = RuleId,
                        Category = Category,
                        Severity = IssueSeverity.Error,
                        Title = "Shader 变体数量过多",
                        Description = $"Shader {path} 的变体数量约为 {variantCount}，超过阈值 {VariantCountThreshold}。",
                        AssetPath = path,
                        AssetGuid = guid,
                        SuggestedFix = "使用 shader_feature 替代 multi_compile，或精简 Keyword 组合。",
                        PotentialSavingsBytes = variantCount * 1024 // 粗略估算
                    });
                }
            }

            // 扫描 ShaderVariantCollection
            var svcGuids = AssetDatabase.FindAssets("t:ShaderVariantCollection", new[] { "Assets" });
            foreach (var guid in svcGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var svc = AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(path);
                if (svc == null) continue;

                // ShaderVariantCollection 的变体数量没有直接 API，可通过反射获取，这里做简化估算
                issues.Add(new DiagnosticIssue
                {
                    RuleId = RuleId,
                    Category = Category,
                    Severity = IssueSeverity.Info,
                    Title = "ShaderVariantCollection 检查",
                    Description = $"请人工检查 {path} 中是否包含未使用的变体。",
                    AssetPath = path,
                    AssetGuid = guid,
                    SuggestedFix = "移除未在场景中实际使用的变体组合。",
                    PotentialSavingsBytes = 0
                });
            }

            // 检查 Always Included Shaders 变体数量
            var alwaysIncluded = GetAlwaysIncludedShaders();
            if (alwaysIncluded != null)
            {
                int totalAlwaysIncludedVariants = 0;
                foreach (var shader in alwaysIncluded)
                {
                    if (shader == null) continue;
                    try
                    {
                        totalAlwaysIncludedVariants += GetShaderVariantCount(shader);
                    }
                    catch { }
                }

                if (totalAlwaysIncludedVariants > AlwaysIncludedVariantThreshold)
                {
                    issues.Add(new DiagnosticIssue
                    {
                        RuleId = RuleId,
                        Category = Category,
                        Severity = IssueSeverity.Warning,
                        Title = "Always Included Shaders 变体过多",
                        Description = $"Always Included Shaders 累计变体数约为 {totalAlwaysIncludedVariants}，会增加首包体积和 WarmUp 耗时。",
                        AssetPath = "ProjectSettings/GraphicsSettings.asset",
                        SuggestedFix = "减少 Always Included Shaders 列表，改用 ShaderVariantCollection 按需预热。",
                        PotentialSavingsBytes = totalAlwaysIncludedVariants * 512
                    });
                }
            }

            return issues;
        }

        private static int GetShaderVariantCount(UnityEngine.Shader shader)
        {
            var method = typeof(ShaderUtil).GetMethod("GetVariantCount", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (method != null)
            {
                return (int)method.Invoke(null, new object[] { shader });
            }

            var legacyMethod = typeof(ShaderUtil).GetMethod("GetShaderVariantCount", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (legacyMethod != null)
            {
                return (int)legacyMethod.Invoke(null, new object[] { shader });
            }

            return 0;
        }

        private static UnityEngine.Shader[] GetAlwaysIncludedShaders()
        {
            try
            {
                var type = System.Type.GetType("UnityEngine.GraphicsSettings, UnityEngine.CoreModule")
                    ?? System.Type.GetType("UnityEngine.GraphicsSettings, UnityEngine");
                if (type == null) return null;

                var prop = type.GetProperty("alwaysIncludedShaders", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (prop == null) return null;

                return prop.GetValue(null) as UnityEngine.Shader[];
            }
            catch
            {
                return null;
            }
        }
    }
}
