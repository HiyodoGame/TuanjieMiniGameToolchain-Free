using System.Collections.Generic;
using System.Linq;

namespace MiniGame.PerformanceSuite.Runtime.Data
{
    /// <summary>
    /// iOS 设备内存上限参考表（基于微信小游戏官方优化建议）。
    /// </summary>
    public static class MPSIOSMemoryLimits
    {
        /// <summary>
        /// 设备标识 -> 建议内存上限（字节）。
        /// </summary>
        public static readonly Dictionary<string, long> DeviceLimits = new Dictionary<string, long>
        {
            // 2GB RAM 机型，建议上限约 1GB
            { "iPhone8,1", 1024L * 1024 * 1024 },  // iPhone 6s
            { "iPhone8,2", 1024L * 1024 * 1024 },  // iPhone 6s Plus
            { "iPhone8,4", 1024L * 1024 * 1024 },  // iPhone SE (1st)
            { "iPhone9,1", 1024L * 1024 * 1024 },  // iPhone 7
            { "iPhone9,2", 1024L * 1024 * 1024 },  // iPhone 7 Plus
            { "iPhone9,3", 1024L * 1024 * 1024 },
            { "iPhone9,4", 1024L * 1024 * 1024 },

            // 3GB RAM 机型，建议上限约 1.4GB
            { "iPhone10,1", 1400L * 1024 * 1024 }, // iPhone 8
            { "iPhone10,2", 1400L * 1024 * 1024 }, // iPhone 8 Plus
            { "iPhone10,3", 1400L * 1024 * 1024 }, // iPhone X
            { "iPhone10,4", 1400L * 1024 * 1024 },
            { "iPhone10,5", 1400L * 1024 * 1024 },
            { "iPhone10,6", 1400L * 1024 * 1024 },
            { "iPhone11,8", 1400L * 1024 * 1024 }, // iPhone XR
            { "iPhone11,2", 1400L * 1024 * 1024 }, // iPhone XS
            { "iPhone11,4", 1400L * 1024 * 1024 }, // iPhone XS Max
            { "iPhone11,6", 1400L * 1024 * 1024 },

            // 4GB RAM 机型，建议上限约 1.8GB
            { "iPhone12,1", 1800L * 1024 * 1024 }, // iPhone 11
            { "iPhone12,3", 1800L * 1024 * 1024 }, // iPhone 11 Pro
            { "iPhone12,5", 1800L * 1024 * 1024 }, // iPhone 11 Pro Max
            { "iPhone12,8", 1800L * 1024 * 1024 }, // iPhone SE (2nd)
            { "iPhone13,1", 1800L * 1024 * 1024 }, // iPhone 12 mini
            { "iPhone13,2", 1800L * 1024 * 1024 }, // iPhone 12
            { "iPhone13,3", 1800L * 1024 * 1024 }, // iPhone 12 Pro
            { "iPhone13,4", 1800L * 1024 * 1024 }, // iPhone 12 Pro Max
        };

        /// <summary>
        /// 获取指定设备标识的内存上限。
        /// </summary>
        public static long? GetLimit(string deviceIdentifier)
        {
            if (string.IsNullOrEmpty(deviceIdentifier)) return null;
            if (DeviceLimits.TryGetValue(deviceIdentifier, out var limit))
            {
                return limit;
            }
            return null;
        }

        /// <summary>
        /// 获取所有已知的设备显示名称列表。
        /// </summary>
        public static string[] GetAllDeviceNames()
        {
            return DeviceLimits.Keys.ToArray();
        }

        /// <summary>
        /// 默认设备上限（iPhone 8 约 1.4GB）。
        /// </summary>
        public static long DefaultLimitBytes => 1400L * 1024 * 1024;
    }
}
