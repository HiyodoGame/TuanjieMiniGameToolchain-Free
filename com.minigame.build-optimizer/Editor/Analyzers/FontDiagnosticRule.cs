using System.Collections.Generic;
using System.IO;
using System.Linq;
using MiniGame.Core.Editor.Analyzers;
using UnityEditor;
using UnityEngine;

namespace MiniGame.BuildOptimizer.Editor.Analyzers
{
    /// <summary>
    /// 字体诊断规则：检测 oversized 字体文件，并提示进行子集裁剪。
    /// </summary>
    public class FontDiagnosticRule : DiagnosticRule
    {
        public long FontSizeThresholdBytes = 2 * 1024 * 1024; // 2MB

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
                        PotentialSavingsBytes = (long)(fileInfo.Length * 0.8f) // 粗略估算裁剪后可节省 80%
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

                if (fileInfo.Length > FontSizeThresholdBytes)
                {
                    issues.Add(new DiagnosticIssue
                    {
                        RuleId = RuleId,
                        Category = Category,
                        Severity = IssueSeverity.Warning,
                        Title = "TMP Font Asset 过大",
                        Description = $"TMP 字体资源 {path} 大小为 {fileInfo.Length / (1024f * 1024f):F2} MB。",
                        AssetPath = path,
                        AssetGuid = guid,
                        SuggestedFix = "重新生成仅包含使用字符的 SDF 图集，或降低图集分辨率。",
                        PotentialSavingsBytes = (long)(fileInfo.Length * 0.6f)
                    });
                }
            }

            return issues;
        }
    }
}
