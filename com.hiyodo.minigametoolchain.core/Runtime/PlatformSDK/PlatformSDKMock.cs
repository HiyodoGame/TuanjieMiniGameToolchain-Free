using System;
using UnityEngine;

namespace MiniGame.Core.Runtime.PlatformSDK
{
    /// <summary>
    /// 平台 SDK 的 Editor / Standalone Mock 实现，便于无需真机即可调试业务流程。
    /// </summary>
    public class PlatformSDKMock : IPlatformSDK
    {
        public void Login(Action<LoginResult> callback)
        {
            callback?.Invoke(new LoginResult
            {
                Success = true,
                Code = "editor_mock_code",
                OpenId = "editor_mock_openid"
            });
        }

        public void Share(ShareConfig config, Action<ShareResult> callback)
        {
            Debug.Log($"[PlatformSDKMock] Share: {config?.Title}");
            callback?.Invoke(new ShareResult { Success = true });
        }

        public void ShowRewardedAd(string adUnitId, Action<AdResult> callback)
        {
            Debug.Log($"[PlatformSDKMock] Rewarded ad: {adUnitId}");
            callback?.Invoke(new AdResult { Success = true, Rewarded = true });
        }

        public void ShowInterstitialAd(string adUnitId, Action<AdResult> callback)
        {
            Debug.Log($"[PlatformSDKMock] Interstitial ad: {adUnitId}");
            callback?.Invoke(new AdResult { Success = true });
        }

        public void ShowBannerAd(string adUnitId, BannerPosition position)
        {
            Debug.Log($"[PlatformSDKMock] Banner shown at {position}: {adUnitId}");
        }

        public void HideBannerAd()
        {
            Debug.Log("[PlatformSDKMock] Banner hidden");
        }

        public void RequestPay(PayConfig config, Action<PayResult> callback)
        {
            Debug.Log($"[PlatformSDKMock] Pay: {config?.ProductName}");
            callback?.Invoke(new PayResult { Success = true, OrderId = "editor_mock_order" });
        }

        public void SetRankData(string key, int score, Action<bool> callback)
        {
            Debug.Log($"[PlatformSDKMock] SetRank: {key}={score}");
            callback?.Invoke(true);
        }

        public void GetRankData(string key, Action<RankData> callback)
        {
            callback?.Invoke(new RankData
            {
                Success = true,
                Key = key,
                Score = 100,
                Rank = 1
            });
        }

        public void SaveToCloud(string key, string data, Action<bool> callback)
        {
            Debug.Log($"[PlatformSDKMock] Cloud save: {key}");
            callback?.Invoke(true);
        }

        public void LoadFromCloud(string key, Action<string> callback)
        {
            Debug.Log($"[PlatformSDKMock] Cloud load: {key}");
            callback?.Invoke("editor_mock_cloud_data");
        }

        public void GetSystemInfo(Action<SystemInfoResult> callback)
        {
            callback?.Invoke(new SystemInfoResult
            {
                Brand = "Editor",
                Model = "PC",
                System = "Windows",
                ScreenWidth = 1920,
                ScreenHeight = 1080,
                Language = "zh-CN",
                Version = "1.0.0"
            });
        }
    }
}
