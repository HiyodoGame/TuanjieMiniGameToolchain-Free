using UnityEngine;
using UnityEngine.Profiling;

namespace MiniGame.PerformanceSuite.Runtime.HUD
{
    /// <summary>
    /// 音频内存估算工具。统计当前加载的所有 AudioClip 运行时内存占用。
    /// 注意：FindObjectsOfTypeAll 开销较大，应低频调用（秒级），不要每帧执行。
    /// </summary>
    public static class MPSAudioMemoryEstimator
    {
        /// <summary>
        /// 估算当前所有 AudioClip 的总内存（字节）。
        /// </summary>
        public static long EstimateBytes()
        {
            long total = 0;
            var clips = Resources.FindObjectsOfTypeAll<AudioClip>();
            foreach (var clip in clips)
            {
                if (clip == null) continue;
                total += Profiler.GetRuntimeMemorySizeLong(clip);
            }

            return total;
        }
    }
}
