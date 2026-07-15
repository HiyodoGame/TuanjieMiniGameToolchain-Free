using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MiniGame.Core.Editor.Analyzers;
using UnityEditor;
using UnityEngine;

namespace MiniGame.BuildOptimizer.Editor.Analyzers
{
    /// <summary>
    /// 字体诊断规则：检测 oversized 字体文件、TMP 图集过大，并提示进行子集裁剪。
    /// </summary>
    public class FontDiagnosticRule : DiagnosticRule
    {
        public long FontSizeThresholdBytes = 2 * 1024 * 1024; // 2MB
        public long TmpAtlasMemoryThresholdBytes = 2 * 1024 * 1024; // 2MB
        public int TmpGlyphCountThreshold = 2048;

        public FontDiagnosticRule()
        {
            RuleId = "FONT_DIAGNOSTIC";
            Category = "Font";
            DefaultSeverity = IssueSeverity.Warning;
            DisplayName = "字体诊断";
        }

        public override List<DiagnosticIssue> Evaluate(DiagnosticContext context)
        {
            var issues = new List<DiagnosticIssue>();

            var fontGuids = AssetDatabase.FindAssets("t:Font", new[] { "Assets" });
            foreach (var guid in fontGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var fileInfo = new FileInfo(path);
                if (!fileInfo.Exists) continue;

                if (fileInfo.Length > FontSizeThresholdBytes)
                {
                    issues.Add(new DiagnosticIssue
                    {
                        RuleId = RuleId,
                        Category = Category,
                        Severity = IssueSeverity.Warning,
                        Title = "字体文件过大",
                        Description = $"字体 {path} 大小为 {fileInfo.Length / (1024f * 1024f):F2} MB，超过 {FontSizeThresholdBytes / (1024f * 1024f):F0} MB 阈值。",
                        AssetPath = path,
                        AssetGuid = guid,
                        SuggestedFix = "使用字体裁剪工具生成仅包含项目实际使用字符的子集字体。",
                        PotentialSavingsBytes = (long)(fileInfo.Length * 0.8f)
                    });
                }
            }

            // 检查 TMP Font Asset
            var tmpFontGuids = AssetDatabase.FindAssets("t:TMP_FontAsset", new[] { "Assets" });
            foreach (var guid in tmpFontGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var fileInfo = new FileInfo(path);
                if (!fileInfo.Exists) continue;

                var info = GetTmpFontAssetInfo(path);
                long atlasMemory = (long)info.AtlasWidth * info.AtlasHeight * 4;
                bool atlasTooLarge = atlasMemory > TmpAtlasMemoryThresholdBytes;
                bool tooManyGlyphs = info.GlyphCount > TmpGlyphCountThreshold;

                if (atlasTooLarge || tooManyGlyphs)
                {
                    var description = $"TMP 字体资源 {path} ";
                    if (atlasTooLarge)
                    {
                        description += $"图集为 {info.AtlasWidth}x{info.AtlasHeight}（约 {atlasMemory / (1024f * 1024f):F2} MB）";
                    }
                    if (tooManyGlyphs)
                    {
                        description += atlasTooLarge ? "，且" : "";
                        description += $"包含 {info.GlyphCount} 个字形";
                    }
                    description += "。";

                    issues.Add(new DiagnosticIssue
                    {
                        RuleId = RuleId,
                        Category = Category,
                        Severity = IssueSeverity.Warning,
                        Title = "TMP Font Asset 过大",
                        Description = description,
                        AssetPath = path,
                        AssetGuid = guid,
                        SuggestedFix = "重新生成仅包含使用字符的 SDF 图集，或降低图集分辨率/采样尺寸。",
                        PotentialSavingsBytes = (long)(atlasMemory * 0.6f)
                    });
                }
            }

            return issues;
        }

        private static TmpFontAssetInfo GetTmpFontAssetInfo(string path)
        {
            var result = new TmpFontAssetInfo();
            try
            {
                var fontAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                if (fontAsset == null) return result;

                var type = fontAsset.GetType();
                var atlasWidthProp = type.GetProperty("atlasWidth");
                var atlasHeightProp = type.GetProperty("atlasHeight");
                if (atlasWidthProp != null)
                    result.AtlasWidth = Convert.ToInt32(atlasWidthProp.GetValue(fontAsset));
                if (atlasHeightProp != null)
                    result.AtlasHeight = Convert.ToInt32(atlasHeightProp.GetValue(fontAsset));

                var glyphTableField = type.GetField("glyphTable");
                if (glyphTableField != null)
                {
                    var glyphTable = glyphTableField.GetValue(fontAsset) as System.Collections.IList;
                    if (glyphTable != null)
                        result.GlyphCount = glyphTable.Count;
                }
                else
                {
                    var glyphTableProp = type.GetProperty("glyphTable");
                    if (glyphTableProp != null)
                    {
                        var glyphTable = glyphTableProp.GetValue(fontAsset) as System.Collections.IList;
                        if (glyphTable != null)
                            result.GlyphCount = glyphTable.Count;
                    }
                }
            }
            catch
            {
                // 反射失败时忽略
            }
            return result;
        }

        private struct TmpFontAssetInfo
        {
            public int AtlasWidth;
            public int AtlasHeight;
            public int GlyphCount;
        }
    }
}
