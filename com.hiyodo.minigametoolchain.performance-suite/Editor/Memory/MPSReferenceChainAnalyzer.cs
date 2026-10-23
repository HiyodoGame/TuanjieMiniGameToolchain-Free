#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MiniGame.PerformanceSuite.Editor.Memory
{
    /// <summary>
    /// 一条引用链：从目标资源出发，逐层向上的引用者名称路径。
    /// </summary>
    public class MPSReferenceChainLink
    {
        /// <summary>路径显示文本（Target ← A ← B）。</summary>
        public string DisplayPath;

        /// <summary>引用深度（1 为直接引用者）。</summary>
        public int Depth;
    }

    /// <summary>
    /// 对象引用链分析器（编辑器专用）。
    /// 通过 SerializedObject 遍历项目内对象，查找谁引用了目标资源，
    /// BFS 逐层向外扩展，默认最大深度 10 层。
    ///
    /// 注意：全量扫描开销较大，按需对单个资源调用，不要在循环中批量执行。
    /// </summary>
    public static class MPSReferenceChainAnalyzer
    {
        private const int DefaultMaxDepth = 10;
        private const int MaxInspections = 3000;

        /// <summary>
        /// 查找目标资源的引用链。
        /// </summary>
        /// <param name="target">目标资源。</param>
        /// <param name="maxDepth">最大向外扩展层数（默认 10）。</param>
        /// <param name="maxResults">最大返回链数。</param>
        /// <param name="maxInspections">最大属性检查次数（扫描预算，防止大项目长时间卡顿）。</param>
        public static List<MPSReferenceChainLink> FindReferenceChains(UnityEngine.Object target, int maxDepth = DefaultMaxDepth, int maxResults = 100, int maxInspections = MaxInspections)
        {
            var results = new List<MPSReferenceChainLink>();
            if (target == null) return results;

            maxDepth = Mathf.Clamp(maxDepth, 1, 10);

            // 缓存一次全量对象列表，避免每层 BFS 重复扫描
            var allObjects = Resources.FindObjectsOfTypeAll<Object>();

            var visited = new HashSet<int> { target.GetInstanceID() };
            var queue = new Queue<KeyValuePair<Object, List<Object>>>();
            queue.Enqueue(new KeyValuePair<Object, List<Object>>(target, new List<Object> { target }));

            int inspections = 0;

            while (queue.Count > 0 && results.Count < maxResults && inspections < maxInspections)
            {
                var pair = queue.Dequeue();
                var current = pair.Key;
                var path = pair.Value;

                if (path.Count - 1 >= maxDepth) continue;

                foreach (var candidate in allObjects)
                {
                    if (candidate == null) continue;
                    int candidateId = candidate.GetInstanceID();
                    if (visited.Contains(candidateId)) continue;
                    if (candidate == current) continue;

                    if (inspections >= maxInspections) break;
                    inspections++;

                    if (!ReferencesObject(candidate, current)) continue;

                    visited.Add(candidateId);
                    var newPath = new List<Object>(path) { candidate };

                    results.Add(new MPSReferenceChainLink
                    {
                        DisplayPath = BuildDisplayPath(newPath),
                        Depth = newPath.Count - 1
                    });

                    queue.Enqueue(new KeyValuePair<Object, List<Object>>(candidate, newPath));
                }
            }

            return results;
        }

        /// <summary>
        /// 判断 container 的序列化属性中是否引用了 target。
        /// </summary>
        public static bool ReferencesObject(Object container, Object target)
        {
            if (container == null || target == null || container == target) return false;

            SerializedObject serialized;
            try
            {
                serialized = new SerializedObject(container);
            }
            catch
            {
                return false;
            }

            using (serialized)
            {
                var property = serialized.GetIterator();
                while (property.NextVisible(true))
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                    if (property.objectReferenceValue == target) return true;
                }
            }

            return false;
        }

        private static string BuildDisplayPath(List<Object> path)
        {
            var parts = new List<string>();
            for (int i = 0; i < path.Count; i++)
            {
                var obj = path[i];
                parts.Add(obj == null ? "?" : $"{obj.GetType().Name} '{obj.name}'");
            }

            return string.Join(" ← ", parts);
        }
    }
}
#endif
