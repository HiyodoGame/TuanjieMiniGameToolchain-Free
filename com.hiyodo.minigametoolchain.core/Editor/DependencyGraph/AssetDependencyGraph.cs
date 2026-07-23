using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MiniGame.Core.Editor.DependencyGraph
{
    /// <summary>
    /// 表示资源依赖图中的一个节点。
    /// </summary>
    [Serializable]
    public class AssetNode
    {
        public string Guid;
        public string Path;
        public long SizeBytes;
        public HashSet<string> Dependencies = new HashSet<string>();
        public HashSet<string> Dependents = new HashSet<string>();

        public AssetNode(string guid, string path)
        {
            Guid = guid;
            Path = path;
        }
    }

    /// <summary>
    /// 基于 AssetDatabase.GetDependencies 构建的项目资源依赖图。
    /// 用于首包分析、自动分包、Unused Asset 检测等场景。
    /// </summary>
    public class AssetDependencyGraph
    {
        private readonly Dictionary<string, AssetNode> _nodes = new Dictionary<string, AssetNode>();
        private readonly bool _recursive;

        /// <summary>
        /// 创建依赖图实例。
        /// </summary>
        /// <param name="recursive">是否递归展开依赖。</param>
        public AssetDependencyGraph(bool recursive = true)
        {
            _recursive = recursive;
        }

        /// <summary>
        /// 获取所有节点。
        /// </summary>
        public IReadOnlyDictionary<string, AssetNode> Nodes => _nodes;

        /// <summary>
        /// 从指定的根资源集合构建依赖图。
        /// </summary>
        /// <param name="rootGuids">根资源 GUID 列表，例如 BuildSettings 中的场景。</param>
        public void BuildFromRoots(IEnumerable<string> rootGuids)
        {
            _nodes.Clear();
            var queue = new Queue<string>(rootGuids.Distinct());
            var visited = new HashSet<string>();

            foreach (var root in queue)
            {
                EnsureNode(root);
            }

            while (queue.Count > 0)
            {
                var guid = queue.Dequeue();
                if (visited.Contains(guid)) continue;
                visited.Add(guid);

                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path)) continue;

                var node = EnsureNode(guid);
                node.Path = path;

                var deps = AssetDatabase.GetDependencies(path, _recursive);
                foreach (var depPath in deps)
                {
                    if (depPath == path) continue;
                    var depGuid = AssetDatabase.AssetPathToGUID(depPath);
                    if (string.IsNullOrEmpty(depGuid)) continue;

                    var depNode = EnsureNode(depGuid);
                    depNode.Path = depPath;
                    node.Dependencies.Add(depGuid);
                    depNode.Dependents.Add(guid);

                    if (!visited.Contains(depGuid))
                    {
                        queue.Enqueue(depGuid);
                    }
                }
            }
        }

        /// <summary>
        /// 从整个 Assets 目录构建依赖图。
        /// </summary>
        public void BuildFromWholeProject()
        {
            var guids = AssetDatabase.FindAssets("", new[] { "Assets" });
            BuildFromRoots(guids);
        }

        /// <summary>
        /// 获取指定 GUID 的节点。
        /// </summary>
        public AssetNode GetNode(string guid)
        {
            _nodes.TryGetValue(guid, out var node);
            return node;
        }

        /// <summary>
        /// 使用 Tarjan 算法检测强连通分量（循环依赖组）。
        /// </summary>
        public List<List<AssetNode>> FindStronglyConnectedComponents()
        {
            var result = new List<List<AssetNode>>();
            var index = 0;
            var stack = new Stack<AssetNode>();
            var onStack = new HashSet<string>();
            var indices = new Dictionary<string, int>();
            var lowlinks = new Dictionary<string, int>();

            void StrongConnect(AssetNode v)
            {
                indices[v.Guid] = index;
                lowlinks[v.Guid] = index;
                index++;
                stack.Push(v);
                onStack.Add(v.Guid);

                foreach (var depGuid in v.Dependencies)
                {
                    if (!_nodes.TryGetValue(depGuid, out var w)) continue;
                    if (!indices.ContainsKey(w.Guid))
                    {
                        StrongConnect(w);
                        lowlinks[v.Guid] = Math.Min(lowlinks[v.Guid], lowlinks[w.Guid]);
                    }
                    else if (onStack.Contains(w.Guid))
                    {
                        lowlinks[v.Guid] = Math.Min(lowlinks[v.Guid], indices[w.Guid]);
                    }
                }

                if (lowlinks[v.Guid] == indices[v.Guid])
                {
                    var component = new List<AssetNode>();
                    AssetNode w;
                    do
                    {
                        w = stack.Pop();
                        onStack.Remove(w.Guid);
                        component.Add(w);
                    } while (w.Guid != v.Guid);

                    if (component.Count > 1)
                    {
                        result.Add(component);
                    }
                }
            }

            foreach (var node in _nodes.Values)
            {
                if (!indices.ContainsKey(node.Guid))
                {
                    StrongConnect(node);
                }
            }

            return result;
        }

        /// <summary>
        /// 获取所有从未被任何根节点（或指定集合）引用的资源 GUID。
        /// </summary>
        /// <param name="rootGuids">被视为"已使用"的根 GUID 集合。</param>
        public HashSet<string> FindUnusedAssets(IEnumerable<string> rootGuids)
        {
            var used = new HashSet<string>();
            var queue = new Queue<string>(rootGuids.Distinct());

            while (queue.Count > 0)
            {
                var guid = queue.Dequeue();
                if (!used.Add(guid)) continue;
                if (!_nodes.TryGetValue(guid, out var node)) continue;

                foreach (var dep in node.Dependencies)
                {
                    queue.Enqueue(dep);
                }
            }

            return new HashSet<string>(_nodes.Keys.Except(used));
        }

        private AssetNode EnsureNode(string guid)
        {
            if (!_nodes.TryGetValue(guid, out var node))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                node = new AssetNode(guid, path);
                _nodes[guid] = node;
            }
            return node;
        }
    }
}
