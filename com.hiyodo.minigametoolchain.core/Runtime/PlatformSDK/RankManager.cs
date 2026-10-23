using System;

namespace MiniGame.Core.Runtime.PlatformSDK
{
    /// <summary>
    /// 排行榜管理器：封装设置/获取排行榜数据。
    /// </summary>
    public class RankManager
    {
        public static RankManager Instance { get; } = new RankManager();

        /// <summary>
        /// 底层平台 SDK。
        /// </summary>
        public IPlatformSDK SDK { get; set; }

        /// <summary>
        /// 初始化排行榜管理器。
        /// </summary>
        public void Initialize(IPlatformSDK sdk = null)
        {
            SDK = sdk ?? PlatformSDKFactory.Create();
        }

        /// <summary>
        /// 上报排行榜分数。
        /// </summary>
        public void SetScore(string key, int score, Action<bool> callback = null)
        {
            if (SDK == null)
            {
                callback?.Invoke(false);
                return;
            }

            SDK.SetRankData(key, score, callback);
        }

        /// <summary>
        /// 获取排行榜数据。
        /// </summary>
        public void GetRank(string key, Action<RankData> callback)
        {
            if (SDK == null)
            {
                callback?.Invoke(new RankData { Success = false, ErrorMessage = "SDK not initialized." });
                return;
            }

            SDK.GetRankData(key, callback);
        }
    }
}
