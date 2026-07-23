using System;

namespace MiniGame.Core.Editor.WeChatSDK
{
    /// <summary>
    /// 微信小游戏 SDK 统一接口。
    /// </summary>
    public interface IWeChatSDK
    {
        void Login(Action<LoginResult> callback);
        void Share(ShareConfig config, Action<ShareResult> callback);
        void ShowRewardedAd(string adUnitId, Action<AdResult> callback);
        void ShowBannerAd(string adUnitId, BannerPosition position);
        void HideBannerAd();
        void GetSystemInfo(Action<SystemInfoResult> callback);
    }

    public class LoginResult
    {
        public bool Success;
        public string Code;
        public string OpenId;
        public string ErrorMessage;
    }

    public class ShareConfig
    {
        public string Title;
        public string ImageUrl;
    }

    public class ShareResult
    {
        public bool Success;
        public string ErrorMessage;
    }

    public class AdResult
    {
        public bool Success;
        public bool Rewarded;
        public string ErrorMessage;
    }

    public enum BannerPosition
    {
        Top,
        Bottom,
        Center
    }

    public class SystemInfoResult
    {
        public string Brand;
        public string Model;
        public string System;
        public int ScreenWidth;
        public int ScreenHeight;
        public string Language;
        public string Version;
    }

    /// <summary>
    /// Editor / Standalone 下的 Mock 实现，便于无需真机即可调试业务流程。
    /// </summary>
    public class WeChatSDKMock : IWeChatSDK
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
            callback?.Invoke(new ShareResult { Success = true });
        }

        public void ShowRewardedAd(string adUnitId, Action<AdResult> callback)
        {
            callback?.Invoke(new AdResult { Success = true, Rewarded = true });
        }

        public void ShowBannerAd(string adUnitId, BannerPosition position)
        {
            UnityEngine.Debug.Log($"[WeChatSDKMock] Banner shown at {position}");
        }

        public void HideBannerAd()
        {
            UnityEngine.Debug.Log("[WeChatSDKMock] Banner hidden");
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
