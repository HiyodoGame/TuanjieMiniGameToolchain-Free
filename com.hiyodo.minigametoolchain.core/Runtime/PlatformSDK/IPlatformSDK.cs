using System;

namespace MiniGame.Core.Runtime.PlatformSDK
{
    /// <summary>
    /// 跨平台小游戏 SDK 抽象层。
    /// 统一微信、抖音、快手等平台的登录、分享、广告、支付、排行榜、云存档接口。
    /// </summary>
    public interface IPlatformSDK
    {
        void Login(Action<LoginResult> callback);
        void Share(ShareConfig config, Action<ShareResult> callback);
        void ShowRewardedAd(string adUnitId, Action<AdResult> callback);
        void ShowInterstitialAd(string adUnitId, Action<AdResult> callback);
        void ShowBannerAd(string adUnitId, BannerPosition position);
        void HideBannerAd();
        void RequestPay(PayConfig config, Action<PayResult> callback);
        void SetRankData(string key, int score, Action<bool> callback);
        void GetRankData(string key, Action<RankData> callback);
        void SaveToCloud(string key, string data, Action<bool> callback);
        void LoadFromCloud(string key, Action<string> callback);
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
        public string Query;
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

    public class PayConfig
    {
        public string ProductId;
        public string ProductName;
        public int PriceCents;
        public string Currency = "CNY";
    }

    public class PayResult
    {
        public bool Success;
        public string OrderId;
        public string ErrorMessage;
    }

    public class RankData
    {
        public bool Success;
        public string Key;
        public int Score;
        public int Rank;
        public string ErrorMessage;
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
}
