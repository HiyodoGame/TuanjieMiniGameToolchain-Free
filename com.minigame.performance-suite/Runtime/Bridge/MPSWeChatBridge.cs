using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace MiniGame.PerformanceSuite.Runtime.Bridge
{
    /// <summary>
    /// 微信小游戏运行时 JSBridge 封装，用于获取真机性能数据。
    /// </summary>
    public static class MPSWeChatBridge
    {
        /// <summary>
        /// 当前是否运行在 WebGL / 微信小游戏环境。
        /// </summary>
        public static bool IsWebGLRuntime
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            get => true;
#else
            get => false;
#endif
        }

        /// <summary>
        /// 获取微信 JS 堆已用大小（字节）。获取失败返回 -1。
        /// </summary>
        public static long GetUsedJSHeapSize()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                double size = GetUsedJSHeapSizeInternal();
                if (double.IsNaN(size) || size < 0)
                {
                    return -1;
                }
                return (long)size;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MPSWeChatBridge] 获取 JS Heap 失败: {ex.Message}");
                return -1;
            }
#else
            return -1;
#endif
        }

        /// <summary>
        /// 获取微信性能时间戳（毫秒级）。失败返回 -1。
        /// </summary>
        public static double GetPerformanceTimestamp()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                return GetPerformanceTimestampInternal();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MPSWeChatBridge] 获取性能时间戳失败: {ex.Message}");
                return -1;
            }
#else
            return -1;
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern double GetUsedJSHeapSizeInternal();

        [DllImport("__Internal")]
        private static extern double GetPerformanceTimestampInternal();
#else
        private static double GetUsedJSHeapSizeInternal() => -1;
        private static double GetPerformanceTimestampInternal() => -1;
#endif
    }
}
