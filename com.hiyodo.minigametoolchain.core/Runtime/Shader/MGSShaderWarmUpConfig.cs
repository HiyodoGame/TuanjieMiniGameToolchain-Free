using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;

namespace MiniGame.Core.Runtime.ShaderWarmUp
{
    /// <summary>
    /// Shader 分帧预热配置。
    /// </summary>
    [CreateAssetMenu(fileName = "ShaderWarmUpConfig", menuName = "MiniGame/Shader WarmUp Config")]
    public class MGSShaderWarmUpConfig : ScriptableObject
    {
        /// <summary>
        /// 单个需要预热的 Shader 变体条目。
        /// </summary>
        [Serializable]
        public class Entry
        {
            public UnityEngine.Shader Shader;
            public string[] Keywords = Array.Empty<string>();
        }

        /// <summary>
        /// 每帧预热的变体数量。
        /// </summary>
        [Tooltip("每帧预热的变体数量，避免主线程卡顿")]
        public int VariantsPerFrame = 5;

        /// <summary>
        /// 是否随场景启动自动预热。
        /// </summary>
        public bool WarmUpOnStart = true;

        /// <summary>
        /// 需要预热变体的 Shader 列表。
        /// </summary>
        public List<Entry> Entries = new List<Entry>();

        /// <summary>
        /// 分帧预热所有配置的 Shader 变体。
        /// </summary>
        public IEnumerator WarmUpAsync(IProgress<float> progress = null, CancellationToken cancellationToken = default)
        {
            var collection = new ShaderVariantCollection();
            FillCollection(collection);

            int total = collection.variantCount;
            if (total == 0)
            {
                progress?.Report(1f);
                yield break;
            }

            bool done = false;
            while (!done)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    progress?.Report((float)collection.warmedUpVariantCount / total);
                    yield break;
                }

                done = collection.WarmUpProgressively(VariantsPerFrame);
                progress?.Report((float)collection.warmedUpVariantCount / total);
                yield return null;
            }
        }

        private void FillCollection(ShaderVariantCollection collection)
        {
            foreach (var entry in Entries)
            {
                if (entry.Shader == null) continue;

                var keywords = entry.Keywords ?? Array.Empty<string>();
                try
                {
                    var variant = new ShaderVariantCollection.ShaderVariant(entry.Shader, PassType.Normal, keywords);
                    collection.Add(variant);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[MGSShaderWarmUp] 跳过无法识别的 Shader 变体 {entry.Shader.name}: {ex.Message}");
                }
            }
        }
    }
}
