using System.Collections.Generic;
using System.Linq;
using MiniGame.Core.Editor.DependencyGraph;
using UnityEditor;
using UnityEngine;

namespace MiniGame.BuildOptimizer.Editor.AssetBundle
{
    /// <summary>
    /// 资源依赖分析与自动分包策略生成器。
    /// </summary>
    public class AssetBundleAnalyzer
    {
        public long FirstPackageMaxSizeBytes = 4 * 1024 * 1024;
        public long PreloadMaxSizeBytes = 5 * 1024 * 1024;
        public int PreloadMaxFileCount = 10;

        /// <summary>
        /// 分析当前项目并生成分包策略。
        /// </summary>
        public BundleStrategy AnalyzeProject()
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToList();
            var sceneGuids = scenes
                .Select(AssetDatabase.AssetPathToGUID)
                .Where(g => !string.IsNullOrEmpty(g))
                .ToList();

            if (sceneGuids.Count == 0)
            {
                return new BundleStrategy
                {
                    Warnings = { "EditorBuildSettings 中没有启用的场景，无法分析分包策略。" }
                };
            }

            var graph = new AssetDependencyGraph();
            graph.BuildFromRoots(sceneGuids);

            var strategy = new BundleStrategy();
            var sharedAssets = IdentifySharedAssets(graph, sceneGuids);
            var scenePrivateAssets = new Dictionary<string, List<string>>();

            // 为每个场景创建私有资源列表
            foreach (var sceneGuid in sceneGuids)
            {
                var privateAssets = new List<string>();
                CollectPrivateAssets(graph, sceneGuid, sceneGuids, sharedAssets, privateAssets, new HashSet<string>());
                scenePrivateAssets[sceneGuid] = privateAssets;
            }

            // 首包：主场景 + 共享资源
            var mainSceneGuid = sceneGuids.First();
            var firstPackageGroup = new AssetBundleGroup
            {
                GroupName = "FirstPackage",
                Type = BundleGroupType.FirstPackage,
                DownloadPriority = 100
            };

            firstPackageGroup.AssetGuids.Add(mainSceneGuid);
            foreach (var shared in sharedAssets)
            {
                firstPackageGroup.AssetGuids.Add(shared);
            }

            // 将主场景私有资源也放入首包（通常主场景资源较小）
            foreach (var asset in scenePrivateAssets[mainSceneGuid])
            {
                if (!firstPackageGroup.AssetGuids.Contains(asset))
                {
                    firstPackageGroup.AssetGuids.Add(asset);
                }
            }

            firstPackageGroup.EstimatedSizeBytes = CalculateSize(firstPackageGroup.AssetGuids);
            strategy.Groups.Add(firstPackageGroup);

            // 如果首包超限，将最大非关键资产移到预下载
            BalanceFirstPackage(strategy);

            // 其他场景作为远程包
            foreach (var sceneGuid in sceneGuids.Skip(1))
            {
                var group = new AssetBundleGroup
                {
                    GroupName = $"Scene_{AssetDatabase.GUIDToAssetPath(sceneGuid).Replace('/', '_')}",
                    Type = BundleGroupType.RemoteCDN,
                    DownloadPriority = 50
                };

                group.AssetGuids.Add(sceneGuid);
                foreach (var asset in scenePrivateAssets[sceneGuid])
                {
                    if (!sharedAssets.Contains(asset))
                    {
                        group.AssetGuids.Add(asset);
                    }
                }

                group.EstimatedSizeBytes = CalculateSize(group.AssetGuids);
                strategy.Groups.Add(group);
            }

            // 重新计算总体积
            strategy.EstimatedFirstPackageSizeBytes = strategy.Groups
                .Where(g => g.Type == BundleGroupType.FirstPackage)
                .Sum(g => g.EstimatedSizeBytes);
            strategy.EstimatedRemotePackageSizeBytes = strategy.Groups
                .Where(g => g.Type == BundleGroupType.RemoteCDN)
                .Sum(g => g.EstimatedSizeBytes);
            strategy.EstimatedPreloadSizeBytes = strategy.Groups
                .Where(g => g.Type == BundleGroupType.Preload)
                .Sum(g => g.EstimatedSizeBytes);

            // 检测循环依赖
            var sccs = graph.FindStronglyConnectedComponents();
            if (sccs.Count > 0)
            {
                strategy.Warnings.Add($"检测到 {sccs.Count} 组循环依赖资源，建议检查并解耦。");
            }

            return strategy;
        }

        /// <summary>
        /// 识别被多个场景共享的资源。
        /// </summary>
        private HashSet<string> IdentifySharedAssets(AssetDependencyGraph graph, List<string> sceneGuids)
        {
            var assetSceneCount = new Dictionary<string, int>();

            foreach (var sceneGuid in sceneGuids)
            {
                var visited = new HashSet<string>();
                var queue = new Queue<string>();
                queue.Enqueue(sceneGuid);

                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    if (!visited.Add(current)) continue;

                    if (current != sceneGuid)
                    {
                        if (!assetSceneCount.ContainsKey(current))
                            assetSceneCount[current] = 0;
                        assetSceneCount[current]++;
                    }

                    var node = graph.GetNode(current);
                    if (node == null) continue;

                    foreach (var dep in node.Dependencies)
                    {
                        queue.Enqueue(dep);
                    }
                }
            }

            return new HashSet<string>(assetSceneCount.Where(kv => kv.Value > 1).Select(kv => kv.Key));
        }

        /// <summary>
        /// 递归收集某个场景的独占资源。
        /// </summary>
        private void CollectPrivateAssets(
            AssetDependencyGraph graph,
            string currentGuid,
            List<string> sceneGuids,
            HashSet<string> sharedAssets,
            List<string> result,
            HashSet<string> visited)
        {
            if (!visited.Add(currentGuid)) return;
            if (sceneGuids.Contains(currentGuid) && currentGuid != sceneGuids.First()) return;
            if (sharedAssets.Contains(currentGuid)) return;

            if (currentGuid != sceneGuids.First())
            {
                result.Add(currentGuid);
            }

            var node = graph.GetNode(currentGuid);
            if (node == null) return;

            foreach (var dep in node.Dependencies)
            {
                CollectPrivateAssets(graph, dep, sceneGuids, sharedAssets, result, visited);
            }
        }

        /// <summary>
        /// 如果首包超限，将最大资产移至预下载。
        /// </summary>
        private void BalanceFirstPackage(BundleStrategy strategy)
        {
            var firstPackage = strategy.Groups.FirstOrDefault(g => g.Type == BundleGroupType.FirstPackage);
            if (firstPackage == null) return;

            var preloadGroup = new AssetBundleGroup
            {
                GroupName = "Preload",
                Type = BundleGroupType.Preload,
                DownloadPriority = 75
            };

            while (firstPackage.EstimatedSizeBytes > FirstPackageMaxSizeBytes && firstPackage.AssetGuids.Count > 1)
            {
                // 排除主场景
                var movable = firstPackage.AssetGuids
                    .Where(g => !EditorBuildSettings.scenes.Any(s => AssetDatabase.AssetPathToGUID(s.path) == g))
                    .OrderByDescending(CalculateSize)
                    .FirstOrDefault();

                if (string.IsNullOrEmpty(movable)) break;

                firstPackage.AssetGuids.Remove(movable);
                preloadGroup.AssetGuids.Add(movable);

                firstPackage.EstimatedSizeBytes = CalculateSize(firstPackage.AssetGuids);
            }

            if (preloadGroup.AssetGuids.Count > 0)
            {
                preloadGroup.EstimatedSizeBytes = CalculateSize(preloadGroup.AssetGuids);

                // 如果预下载包过大，给出警告但仍保留
                if (preloadGroup.EstimatedSizeBytes > PreloadMaxSizeBytes)
                {
                    strategy.Warnings.Add($"预下载包大小 {preloadGroup.EstimatedSizeBytes / (1024f * 1024f):F2} MB 超过建议上限 {PreloadMaxSizeBytes / (1024f * 1024f):F0} MB。");
                }

                if (preloadGroup.AssetGuids.Count > PreloadMaxFileCount)
                {
                    strategy.Warnings.Add($"预下载包文件数 {preloadGroup.AssetGuids.Count} 超过建议上限 {PreloadMaxFileCount}。");
                }

                strategy.Groups.Add(preloadGroup);
            }
        }

        private static long CalculateSize(IEnumerable<string> guids)
        {
            long total = 0;
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path)) continue;

                var fileInfo = new System.IO.FileInfo(path);
                if (fileInfo.Exists)
                {
                    total += fileInfo.Length;
                }
            }
            return total;
        }

        private static long CalculateSize(string guid)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) return 0;

            var fileInfo = new System.IO.FileInfo(path);
            return fileInfo.Exists ? fileInfo.Length : 0;
        }
    }
}
