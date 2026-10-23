using System;

namespace MiniGame.PerformanceSuite.Runtime.Startup
{
    /// <summary>
    /// 单次资源加载事件，用于启动期资源加载瀑布图。
    /// </summary>
    [Serializable]
    public class MPSResourceLoadEvent
    {
        /// <summary>
        /// 资源名称或路径。
        /// </summary>
        public string Name;

        /// <summary>
        /// 加载类别（Scene / AssetBundle / Resources / Shader / Other）。
        /// </summary>
        public string Category;

        /// <summary>
        /// 加载开始时间戳（秒，基于 Time.realtimeSinceStartup）。
        /// </summary>
        public double StartTime;

        /// <summary>
        /// 加载耗时（秒）。负数表示尚未完成。
        /// </summary>
        public double Duration = -1.0;

        /// <summary>
        /// 资源大小（字节），未知时为 0。
        /// </summary>
        public long SizeBytes;

        /// <summary>
        /// 是否已完成。
        /// </summary>
        public bool IsComplete => Duration >= 0.0;
    }

    /// <summary>
    /// 资源加载类别常量。
    /// </summary>
    public static class MPSResourceLoadCategory
    {
        public const string Scene = "Scene";
        public const string AssetBundle = "AssetBundle";
        public const string Resources = "Resources";
        public const string Shader = "Shader";
        public const string Network = "Network";
        public const string Other = "Other";
    }
}
