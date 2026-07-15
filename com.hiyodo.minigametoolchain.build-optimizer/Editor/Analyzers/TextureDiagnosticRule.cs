using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using MiniGame.Core.Editor.Analyzers;
using UnityEditor;
using UnityEngine;

namespace MiniGame.BuildOptimizer.Editor.Analyzers
{
    /// <summary>
    /// 纹理诊断规则：检测未压缩纹理、超大纹理、重复纹理、可读纹理、非 2 的幂等问题。
    /// </summary>
    public class TextureDiagnosticRule : DiagnosticRule
    {
        public int MaxTextureSizeWarning = 2048;
        public int MaxTextureSizeError = 4096;

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

                // 未压缩 / 压缩效果差
                if (!TextureMemoryEstimator.IsCompressed(importer))
                {
                    issues.Add(new DiagnosticIssue
                    {
                        RuleId = RuleId,
                        Category = Category,
                        Severity = IssueSeverity.Error,
                        Title = "未压缩纹理",
                        Description = $"纹理 {path} 未使用压缩格式，会显著增加首包体积和内存占用。",
                        AssetPath = path,
                        AssetGuid = guid,
                        SuggestedFix = "在 Texture Importer 中启用压缩格式（ASTC/ETC2/DXT），或使用 Crunch Compression。",
                        PotentialSavingsBytes = Math.Max(0, TextureMemoryEstimator.EstimateMemorySize(texture, importer) / 2),
                        AutoFixable = true,
                        FixKey = "Texture.Compress"
                    });
                }

                // 可读纹理会额外保留一份 CPU 副本
                if (importer.isReadable)
                {
                    issues.Add(new DiagnosticIssue
                    {
                        RuleId = RuleId,
                        Category = Category,
                        Severity = IssueSeverity.Warning,
                        Title = "纹理标记为 Read/Write",
                        Description = $"纹理 {path} 启用了 Read/Write，会同时占用 CPU 与 GPU 内存。",
                        AssetPath = path,
                        AssetGuid = guid,
                        SuggestedFix = "除非运行时确实需要读取像素，否则关闭 Read/Write Enabled。",
                        PotentialSavingsBytes = TextureMemoryEstimator.EstimateMemorySize(texture, importer),
                        AutoFixable = true,
                        FixKey = "Texture.DisableReadable"
                    });
                }

                // 超大纹理
                var maxSide = Mathf.Max(texture.width, texture.height);
                if (maxSide > MaxTextureSizeError)
                {
                    var resized = TextureMemoryEstimator.EstimateResizedMemory(texture, importer, MaxTextureSizeWarning);
                    var current = TextureMemoryEstimator.EstimateMemorySize(texture, importer);
                    issues.Add(new DiagnosticIssue
                    {
                        RuleId = RuleId,
                        Category = Category,
                        Severity = IssueSeverity.Error,
                        Title = "超大纹理",
                        Description = $"纹理 {path} 尺寸为 {texture.width}x{texture.height}，远超微信小游戏建议上限 {MaxTextureSizeWarning}。",
                        AssetPath = path,
                        AssetGuid = guid,
                        SuggestedFix = $"将最大边缩放到 {MaxTextureSizeWarning} 或更低。",
                        PotentialSavingsBytes = Math.Max(0, current - resized),
                        AutoFixable = true,
                        FixKey = "Texture.LimitSize",
                        FixData = MaxTextureSizeWarning.ToString()
                    });
                }
                else if (maxSide > MaxTextureSizeWarning)
                {
                    var resized = TextureMemoryEstimator.EstimateResizedMemory(texture, importer, MaxTextureSizeWarning);
                    var current = TextureMemoryEstimator.EstimateMemorySize(texture, importer);
                    issues.Add(new DiagnosticIssue
                    {
                        RuleId = RuleId,
                        Category = Category,
                        Severity = IssueSeverity.Warning,
                        Title = "纹理尺寸偏大",
                        Description = $"纹理 {path} 尺寸为 {texture.width}x{texture.height}，超过微信小游戏建议上限 {MaxTextureSizeWarning}。",
                        AssetPath = path,
                        AssetGuid = guid,
                        SuggestedFix = $"将最大边缩放到 {MaxTextureSizeWarning} 或更低。",
                        PotentialSavingsBytes = Math.Max(0, current - resized),
                        AutoFixable = true,
                        FixKey = "Texture.LimitSize",
                        FixData = MaxTextureSizeWarning.ToString()
                    });
                }

                // 非 2 的幂且开启 Mipmap
                if (importer.mipmapEnabled &&
                    (!TextureMemoryEstimator.IsPowerOfTwo(texture.width) || !TextureMemoryEstimator.IsPowerOfTwo(texture.height)))
                {
                    issues.Add(new DiagnosticIssue
                    {
                        RuleId = RuleId,
                        Category = Category,
                        Severity = IssueSeverity.Info,
                        Title = "非 2 的幂纹理开启 Mipmap",
                        Description = $"纹理 {path} 尺寸为 {texture.width}x{texture.height} 且开启了 Mipmap，在部分平台可能导致额外内存开销。",
                        AssetPath = path,
                        AssetGuid = guid,
                        SuggestedFix = "关闭 Mipmap，或将尺寸调整为 2 的幂。",
                        PotentialSavingsBytes = TextureMemoryEstimator.EstimateMemorySize(texture, importer) / 4,
                        AutoFixable = true,
                        FixKey = "Texture.DisableMipmapForNpot"
                    });
                }
            }

            // 重复纹理检测：使用 AssetDependencyHash 精确判断内容是否相同
            var duplicates = FindDuplicateTextures(guids);
            foreach (var group in duplicates)
            {
                var paths = string.Join("\n", group.Select(g => AssetDatabase.GUIDToAssetPath(g)));
                var totalFileSize = group.Sum(g => EstimateFileSize(g));
                var keepOneSize = EstimateFileSize(group.First());
                issues.Add(new DiagnosticIssue
                {
                    RuleId = RuleId,
                    Category = Category,
                    Severity = IssueSeverity.Warning,
                    Title = "重复纹理",
                    Description = $"发现 {group.Count} 个内容完全相同的纹理：\n{paths}",
                    AssetPath = AssetDatabase.GUIDToAssetPath(group.First()),
                    AssetGuid = group.First(),
                    SuggestedFix = "删除重复资源，统一引用同一个纹理。",
                    PotentialSavingsBytes = Math.Max(0, totalFileSize - keepOneSize)
                });
            }

            return issues;
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
            var hashToGuids = new Dictionary<string, List<string>>();

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!File.Exists(path)) continue;

                var hash = ComputeFileHash(path);
                if (string.IsNullOrEmpty(hash)) continue;

                if (!hashToGuids.TryGetValue(hash, out var list))
                {
                    list = new List<string>();
                    hashToGuids[hash] = list;
                }
                list.Add(guid);
            }

            return hashToGuids.Values.Where(g => g.Count > 1).ToList();
        }

        private static string ComputeFileHash(string path)
        {
            try
            {
                using (var md5 = MD5.Create())
                using (var stream = File.OpenRead(path))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "");
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
