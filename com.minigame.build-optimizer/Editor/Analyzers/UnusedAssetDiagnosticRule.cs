using System.Collections.Generic;
using System.Linq;
using MiniGame.Core.Editor.Analyzers;
using MiniGame.Core.Editor.DependencyGraph;
using UnityEditor;

namespace MiniGame.BuildOptimizer.Editor.Analyzers
{
    /// <summary>
    /// 未引用资源诊断规则：基于依赖图识别未被任何构建场景引用的资源。
    /// </summary>
    public class UnusedAssetDiagnosticRule : DiagnosticRule
    {
        public UnusedAssetDiagnosticRule()
        {
            RuleId = "UNUSED_ASSET_DIAGNOSTIC";
            Category = "Unused";
            DefaultSeverity = IssueSeverity.Info;
            DisplayName = "未引用资源诊断";
        }

        public override List<DiagnosticIssue> Evaluate(DiagnosticContext context)
        {
            var issues = new List<DiagnosticIssue>();

            var scenes = EditorBuildSettings.scenes;
            var sceneGuids = scenes
                .Where(s => s.enabled)
                .Select(s => AssetDatabase.AssetPathToGUID(s.path))
                .Where(g => !string.IsNullOrEmpty(g))
                .ToList();

            if (sceneGuids.Count == 0)
            {
                issues.Add(new DiagnosticIssue
                {
                    RuleId = RuleId,
                    Category = Category,
                    Severity = IssueSeverity.Info,
                    Title = "无启用场景",
                    Description = "EditorBuildSettings 中没有启用的场景，无法判断资源是否被引用。"
                });
                return issues;
            }

            var graph = new AssetDependencyGraph();
            graph.BuildFromRoots(sceneGuids);
            var unused = graph.FindUnusedAssets(sceneGuids);

            foreach (var guid in unused)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path)) continue;

                // 跳过脚本、编辑器工具和元数据文件
                if (path.StartsWith("Assets/Scripts/") ||
                    path.Contains("/Editor/") ||
                    path.EndsWith(".asmdef") ||
                    path.EndsWith(".cs"))
                {
                    continue;
                }

                issues.Add(new DiagnosticIssue
                {
                    RuleId = RuleId,
                    Category = Category,
                    Severity = IssueSeverity.Info,
                    Title = "未引用资源",
                    Description = $"资源 {path} 未被任何构建场景直接或间接引用。",
                    AssetPath = path,
                    AssetGuid = guid,
                    SuggestedFix = "如确认不再需要，可删除该资源。"
                });
            }

            return issues;
        }
    }
}
