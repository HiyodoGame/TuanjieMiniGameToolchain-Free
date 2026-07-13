using System.Collections.Generic;
using System.IO;
using System.Linq;
using MiniGame.Core.Editor.Analyzers;
using UnityEditor;
using UnityEngine;

namespace MiniGame.BuildOptimizer.Editor.Analyzers
{
    /// <summary>
    /// 纹理诊断规则：检测未压缩纹理、超大纹理和重复纹理。
    /// </summary>
    public class TextureDiagnosticRule : DiagnosticRule
    {
        public TextureDiagnosticRule()
        {
            RuleId = "TEXTURE_DIAGNOSTIC";
            Category = "Texture";
            DefaultSeverity = IssueSeverity.Warning;
            DisplayName = "纹理诊断";
        }

        public override List<DiagnosticIssue> Evaluate(DiagnosticContext context)
        {
            var issues = new List<DiagnosticIssue>();
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" });

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (importer == null || texture == null) continue;

                // 未压缩纹理
                if (importer.textureCompression == TextureImporterCompression.Uncompressed)
                {
                    issues.Add(new DiagnosticIssue
                    {
                        RuleId = RuleId,
                        Category = Category,
                        Severity = IssueSeverity.Error,
                        Title = "未压缩纹理",
                        Description = $"纹理 {path} 未启用压缩，会显著增加首包体积和内存占用。",
                        AssetPath = path,
                        AssetGuid = guid,
                        SuggestedFix = "在 Texture Importer 中启用压缩格式（ASTC/ETC2/DXT）。",
                        PotentialSavingsBytes = EstimateMemorySize(texture)
                    });
                }

                // 超大纹理
                if (texture.width > 2048 || texture.height > 2048)
                {
                    issues.Add(new DiagnosticIssue
                    {
                        RuleId = RuleId,
                        Category = Category,
                        Severity = IssueSeverity.Warning,
                        Title = "超大纹理",
                        Description = $"纹理 {path} 尺寸为 {texture.width}x{texture.height}，超过微信小游戏建议上限 2048。",
                        AssetPath = path,
                        AssetGuid = guid,
                        SuggestedFix = "将最大边缩放到 2048 或更低。",
                        PotentialSavingsBytes = EstimateMemorySize(texture) / 2
                    });
                }
            }

            // 重复纹理检测（简化版：仅比较文件大小 + 宽高 + 格式）
            var duplicates = FindDuplicateTextures(guids);
            foreach (var group in duplicates)
            {
                var paths = string.Join("\n", group.Select(g => AssetDatabase.GUIDToAssetPath(g)));
                issues.Add(new DiagnosticIssue
                {
                    RuleId = RuleId,
                    Category = Category,
                    Severity = IssueSeverity.Warning,
                    Title = "重复纹理",
                    Description = $"发现 {group.Count} 个内容可能重复的纹理：\n{paths}",
                    AssetPath = AssetDatabase.GUIDToAssetPath(group.First()),
                    AssetGuid = group.First(),
                    SuggestedFix = "删除重复资源，统一引用同一个纹理。",
                    PotentialSavingsBytes = EstimateFileSize(group.First()) * (group.Count - 1)
                });
            }

            return issues;
        }

        private static long EstimateMemorySize(Texture2D texture)
        {
            // 粗略估算：RGBA32 = 4 bytes/pixel
            return (long)texture.width * texture.height * 4;
        }

        private static long EstimateFileSize(string guid)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (File.Exists(path))
            {
                return new FileInfo(path).Length;
            }
            return 0;
        }

        private static List<List<string>> FindDuplicateTextures(string[] guids)
        {
            var fingerprintToGuids = new Dictionary<string, List<string>>();

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture == null) continue;

                var fingerprint = $"{texture.width}x{texture.height}_{texture.format}_{EstimateFileSize(guid)}";
                if (!fingerprintToGuids.TryGetValue(fingerprint, out var list))
                {
                    list = new List<string>();
                    fingerprintToGuids[fingerprint] = list;
                }
                list.Add(guid);
            }

            return fingerprintToGuids.Values.Where(g => g.Count > 1).ToList();
        }
    }
}
