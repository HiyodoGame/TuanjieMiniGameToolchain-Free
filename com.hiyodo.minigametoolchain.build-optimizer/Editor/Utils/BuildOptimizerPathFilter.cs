using System;

namespace MiniGame.BuildOptimizer.Editor.Utils
{
    /// <summary>
    /// Build Optimizer 扫描路径过滤工具。
    /// 用于排除常见 SDK / 插件 / Editor / StreamingAssets 目录，减少误报。
    /// </summary>
    public static class BuildOptimizerPathFilter
    {
        /// <summary>
        /// 默认排除的路径子串（不区分大小写）。
        /// </summary>
        public static readonly string[] DefaultIgnoredPatterns =
        {
            "/WX-WASM-SDK",
            "/Plugins",
            "/StreamingAssets",
            "/Editor/",
            "/Editor Default Resources/",
            "/Gizmos/",
            "/WebGLTemplates/",
            "/Resources/",       // Resources 内资源视为已使用
        };

        /// <summary>
        /// 判断给定资源路径是否应被排除。
        /// </summary>
        public static bool IsIgnored(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return false;

            foreach (var pattern in DefaultIgnoredPatterns)
            {
                if (assetPath.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }
    }
}
