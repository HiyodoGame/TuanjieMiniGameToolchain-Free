using System.Collections.Generic;
using System.IO;
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

            var rootGuids = new HashSet<string>();

            // 1. 将 BuildSettings 中启用的场景作为根
            var scenes = EditorBuildSettings.scenes;
            foreach (var scene in scenes)
            {
                if (!scene.enabled) continue;
                var guid = AssetDatabase.AssetPathToGUID(scene.path);
                if (!string.IsNullOrEmpty(guid))
                    rootGuids.Add(guid);
            }

            // 2. 将 Resources 目录下所有资源作为根（动态加载资源）
            var resourcesGuids = AssetDatabase.FindAssets("", new[] { "Assets" })
                .Select(g => AssetDatabase.GUIDToAssetPath(g))
                .Where(p => p.Replace("\\", "/").Contains("/Resources/"))
                .Select(p => AssetDatabase.AssetPathToGUID(p))
                .Where(g => !string.IsNullOrEmpty(g));
            foreach (var guid in resourcesGuids)
                rootGuids.Add(guid);

            // 3. 将 Addressables 中标记为可寻址的资源作为根（如果安装了 Addressables）
            var addressableGuids = CollectAddressableGuids();
            foreach (var guid in addressableGuids)
                rootGuids.Add(guid);

            if (rootGuids.Count == 0)
            {
                issues.Add(new DiagnosticIssue
                {
                    RuleId = RuleId,
                    Category = Category,
                    Severity = IssueSeverity.Info,
                    Title = "无启用场景或动态加载资源",
                    Description = "EditorBuildSettings 中没有启用的场景，且未发现 Resources/Addressables 资源，无法判断资源是否被引用。"
                });
                return issues;
            }

            var graph = new AssetDependencyGraph();
            graph.BuildFromWholeProject();
            var unused = graph.FindUnusedAssets(rootGuids);

            foreach (var guid in unused)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path)) continue;

                // 跳过非项目资产、目录、脚本、编辑器工具、元数据和场景
                if (string.IsNullOrEmpty(path) ||
                    path.StartsWith("Packages/") ||
                    Directory.Exists(path) ||
                    path.StartsWith("Assets/Scripts/") ||
                    path.Contains("/Editor/") ||
                    path.EndsWith(".asmdef") ||
                    path.EndsWith(".cs") ||
                    path.EndsWith(".unity"))
                {
                    continue;
                }

                long size = EstimateFileSize(path);
                issues.Add(new DiagnosticIssue
                {
                    RuleId = RuleId,
                    Category = Category,
                    Severity = IssueSeverity.Info,
                    Title = "未引用资源",
                    Description = $"资源 {path} 未被任何构建场景直接或间接引用。",
                    AssetPath = path,
                    AssetGuid = guid,
                    SuggestedFix = "如确认不再需要，可删除该资源。",
                    PotentialSavingsBytes = size
                });
            }

            return issues;
        }

        /// <summary>
        /// 通过反射收集 Addressables 中标记为可寻址的资源 GUID。
        /// 未安装 Addressables 时返回空集合，避免编译期依赖。
        /// </summary>
        private static HashSet<string> CollectAddressableGuids()
        {
            var result = new HashSet<string>();
            try
            {
                var settingsType = System.Type.GetType("UnityEditor.AddressableAssets.Settings.AddressableAssetSettings, AddressableAssetsEditor");
                if (settingsType == null) return result;

                var settingsProp = settingsType.GetProperty("DefaultSettings", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                var settings = settingsProp?.GetValue(null);
                if (settings == null) return result;

                var groupsProp = settingsType.GetProperty("groups");
                var groups = groupsProp?.GetValue(settings) as System.Collections.IEnumerable;
                if (groups == null) return result;

                foreach (var group in groups)
                {
                    var entriesProp = group.GetType().GetProperty("entries");
                    var entries = entriesProp?.GetValue(group) as System.Collections.IEnumerable;
                    if (entries == null) continue;

                    foreach (var entry in entries)
                    {
                        var guidProp = entry.GetType().GetProperty("guid");
                        var guid = guidProp?.GetValue(entry) as string;
                        if (!string.IsNullOrEmpty(guid))
                            result.Add(guid);
                    }
                }
            }
            catch
            {
                // 忽略任何反射/版本差异错误
            }
            return result;
        }

        private static long EstimateFileSize(string path)
        {
            try
            {
                if (File.Exists(path))
                    return new FileInfo(path).Length;
            }
            catch
            {
                // 忽略无法读取的文件
            }
            return 0;
        }
    }
}
