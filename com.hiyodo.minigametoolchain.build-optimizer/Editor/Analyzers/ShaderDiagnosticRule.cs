using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MiniGame.Core.Editor.Analyzers;
using UnityEditor;
using UnityEngine;

namespace MiniGame.BuildOptimizer.Editor.Analyzers
{
    /// <summary>
    /// Shader 诊断规则：检测变体数量爆炸、冗余 ShaderVariantCollection 变体、内置重 Shader 等。
    /// </summary>
    public class ShaderDiagnosticRule : DiagnosticRule
    {
        public int VariantCountThreshold = 1000;
        public int AlwaysIncludedVariantThreshold = 100;
        public int MultiCompileLineThreshold = 5;

        private static readonly HashSet<string> HeavyBuiltInShaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Standard",
            "Standard (Specular setup)",
            "Legacy Shaders/",
            "Particles/Standard",
            "Particles/Standard Surface",
            "Particles/Standard Unlit",
            "Nature/",
            "Skybox/"
        };

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
                        PotentialSavingsBytes = variantCount * 512
                    });
                }

                // 检测 multi_compile 导致的变体爆炸
                var multiCompileEstimate = EstimateMultiCompileVariants(path);
                if (multiCompileEstimate.MultiCompileLineCount > MultiCompileLineThreshold &&
                    multiCompileEstimate.EstimatedVariantCount > VariantCountThreshold)
                {
                    issues.Add(new DiagnosticIssue
                    {
                        RuleId = RuleId,
                        Category = Category,
                        Severity = IssueSeverity.Warning,
                        Title = "Shader 使用过多 multi_compile",
                        Description = $"Shader {path} 包含 {multiCompileEstimate.MultiCompileLineCount} 处 multi_compile，粗略估算可能产生 {multiCompileEstimate.EstimatedVariantCount} 个变体。",
                        AssetPath = path,
                        AssetGuid = guid,
                        SuggestedFix = "能用 shader_feature 的地方避免 multi_compile；合并互斥 keyword。",
                        PotentialSavingsBytes = multiCompileEstimate.EstimatedVariantCount * 512
                    });
                }

                // 检测重 Shader 被放入 Always Included
                if (IsHeavyBuiltInShader(shader.name))
                {
                    issues.Add(new DiagnosticIssue
                    {
                        RuleId = RuleId,
                        Category = Category,
                        Severity = IssueSeverity.Warning,
                        Title = "检测到较重的内置 Shader",
                        Description = $"Shader {shader.name}（{path}）体积与变体数通常较大，不建议在微信小游戏中使用。",
                        AssetPath = path,
                        AssetGuid = guid,
                        SuggestedFix = "替换为自定义简化 Shader，避免 Standard / Legacy / Particles 等内置 Shader。",
                        PotentialSavingsBytes = 3 * 1024 * 1024
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

                int svcVariantCount = GetShaderVariantCollectionVariantCount(svc);
                if (svcVariantCount > 0)
                {
                    var severity = svcVariantCount > VariantCountThreshold ? IssueSeverity.Error : IssueSeverity.Info;
                    issues.Add(new DiagnosticIssue
                    {
                        RuleId = RuleId,
                        Category = Category,
                        Severity = severity,
                        Title = "ShaderVariantCollection 变体过多",
                        Description = $"ShaderVariantCollection {path} 包含 {svcVariantCount} 个变体。",
                        AssetPath = path,
                        AssetGuid = guid,
                        SuggestedFix = "仅保留场景中实际使用的变体组合，删除冗余 keyword。",
                        PotentialSavingsBytes = svcVariantCount * 512
                    });
                }
                else
                {
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
            }

            // 检查 Always Included Shaders 变体数量
            var alwaysIncluded = GetAlwaysIncludedShaders();
            if (alwaysIncluded != null)
            {
                int totalAlwaysIncludedVariants = 0;
                var heavyInAlwaysIncluded = new List<string>();
                foreach (var shader in alwaysIncluded)
                {
                    if (shader == null) continue;
                    try
                    {
                        totalAlwaysIncludedVariants += GetShaderVariantCount(shader);
                    }
                    catch { }

                    if (IsHeavyBuiltInShader(shader.name))
                    {
                        heavyInAlwaysIncluded.Add(shader.name);
                    }
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

                if (heavyInAlwaysIncluded.Count > 0)
                {
                    issues.Add(new DiagnosticIssue
                    {
                        RuleId = RuleId,
                        Category = Category,
                        Severity = IssueSeverity.Error,
                        Title = "Always Included 包含重 Shader",
                        Description = $"Always Included Shaders 中包含较重的内置 Shader：{string.Join(", ", heavyInAlwaysIncluded)}。",
                        AssetPath = "ProjectSettings/GraphicsSettings.asset",
                        SuggestedFix = "将重 Shader 从 Always Included 列表移除，改用轻量 Shader 或 ShaderVariantCollection。",
                        PotentialSavingsBytes = heavyInAlwaysIncluded.Count * 2 * 1024 * 1024
                    });
                }
            }

            return issues;
        }

        private static bool IsHeavyBuiltInShader(string shaderName)
        {
            if (string.IsNullOrEmpty(shaderName)) return false;
            return HeavyBuiltInShaders.Any(prefix => shaderName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        }

        private static MultiCompileEstimate EstimateMultiCompileVariants(string path)
        {
            var result = new MultiCompileEstimate();
            if (!File.Exists(path)) return result;

            try
            {
                var source = File.ReadAllText(path);
                // 匹配 #pragma multi_compile ... 与 #pragma multi_compile_local ...
                var matches = Regex.Matches(source, @"#pragma\s+multi_compile(?:_local|_fwdbase|_fwdadd|_lodfade)?\s+(.+)");
                long product = 1;
                foreach (Match match in matches)
                {
                    if (!match.Success) continue;
                    var keywords = match.Groups[1].Value
                        .Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                        .Where(k => !k.StartsWith("//"))
                        .ToList();

                    // multi_compile 行首的 _ 表示默认变体，仍计入 keyword 数
                    int count = Math.Max(1, keywords.Count);
                    result.MultiCompileLineCount++;
                    product *= count;
                }

                result.EstimatedVariantCount = product;
            }
            catch
            {
                // 读取失败时忽略
            }

            return result;
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

        private static int GetShaderVariantCollectionVariantCount(ShaderVariantCollection svc)
        {
            try
            {
                var so = new SerializedObject(svc);
                var shadersProp = so.FindProperty("m_Shaders");
                if (shadersProp == null) return 0;

                int total = 0;
                for (int i = 0; i < shadersProp.arraySize; i++)
                {
                    var shaderEntry = shadersProp.GetArrayElementAtIndex(i);
                    var variantsProp = shaderEntry.FindPropertyRelative("second.variants")
                        ?? shaderEntry.FindPropertyRelative("variants");
                    if (variantsProp != null)
                    {
                        total += variantsProp.arraySize;
                    }
                }
                return total;
            }
            catch
            {
                return 0;
            }
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

        private struct MultiCompileEstimate
        {
            public int MultiCompileLineCount;
            public long EstimatedVariantCount;
        }
    }
}
