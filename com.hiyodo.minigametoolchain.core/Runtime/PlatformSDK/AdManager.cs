using System;
using UnityEngine;

namespace MiniGame.Core.Runtime.PlatformSDK
{
    /// <summary>
    /// 广告管理器：封装激励视频、插屏、Banner 广告生命周期。
    /// </summary>
    public class AdManager
    {
        public static AdManager Instance { get; } = new AdManager();

        /// <summary>
        /// 底层平台 SDK。
        /// </summary>
        public IPlatformSDK SDK { get; set; }

        /// <summary>
        /// 激励视频广告冷却时间（秒）。
        /// </summary>
        public int RewardedAdCooldownSeconds = 5;

        /// <summary>
        /// 每会话最多展示激励视频次数。
        /// </summary>
        public int MaxRewardedShowsPerSession = 100;

        /// <summary>
        /// 激励视频关闭事件（参数：是否获得奖励）。
        /// </summary>
        public event Action<bool> OnRewardedAdClosed;

        /// <summary>
        /// Banner 显示事件。
        /// </summary>
        public event Action OnBannerAdShown;

        /// <summary>
        /// Banner 隐藏事件。
        /// </summary>
        public event Action OnBannerAdHidden;

        private DateTime? _lastRewardedShowTime;
        private int _rewardedShowCount;
        private string _currentBannerAdUnitId;
        private BannerPosition _currentBannerPosition;

        /// <summary>
        /// 使用平台 SDK 初始化广告管理器。
        /// 若未传入 SDK，则自动通过 PlatformSDKFactory 创建。
        /// </summary>
        public void Initialize(IPlatformSDK sdk = null)
        {
            SDK = sdk ?? PlatformSDKFactory.Create();
        }

        /// <summary>
        /// 展示激励视频广告。
        /// </summary>
        public void ShowRewardedAd(string adUnitId, Action<bool> onRewarded = null)
        {
            if (SDK == null)
            {
                Debug.LogWarning("[AdManager] SDK not initialized.");
                onRewarded?.Invoke(false);
                return;
            }

            if (_rewardedShowCount >= MaxRewardedShowsPerSession)
            {
                Debug.LogWarning("[AdManager] Max rewarded shows reached.");
                onRewarded?.Invoke(false);
                return;
            }

            if (_lastRewardedShowTime.HasValue &&
                (DateTime.Now - _lastRewardedShowTime.Value).TotalSeconds < RewardedAdCooldownSeconds)
            {
                Debug.LogWarning("[AdManager] Rewarded ad in cooldown.");
                onRewarded?.Invoke(false);
                return;
            }

            SDK.ShowRewardedAd(adUnitId, result =>
            {
                _lastRewardedShowTime = DateTime.Now;
                if (result.Success && result.Rewarded)
                {
                    _rewardedShowCount++;
                }

                OnRewardedAdClosed?.Invoke(result.Rewarded);
                onRewarded?.Invoke(result.Rewarded);
            });
        }

        /// <summary>
        /// 展示插屏广告。
        /// </summary>
        public void ShowInterstitialAd(string adUnitId, Action<bool> onClosed = null)
        {
            if (SDK == null)
            {
                onClosed?.Invoke(false);
                return;
            }

            SDK.ShowInterstitialAd(adUnitId, result =>
            {
                onClosed?.Invoke(result.Success);
            });
        }

        /// <summary>
        /// 展示 Banner 广告。
        /// </summary>
        public void ShowBannerAd(string adUnitId, BannerPosition position)
        {
            if (SDK == null) return;

            _currentBannerAdUnitId = adUnitId;
            _currentBannerPosition = position;
            SDK.ShowBannerAd(adUnitId, position);
            OnBannerAdShown?.Invoke();
        }

        /// <summary>
        /// 隐藏 Banner 广告。
        /// </summary>
        public void HideBannerAd()
        {
            if (SDK == null) return;

            SDK.HideBannerAd();
            OnBannerAdHidden?.Invoke();
        }

        /// <summary>
        /// 重置激励视频展示计数。
        /// </summary>
        public void ResetRewardedShowCount()
        {
            _rewardedShowCount = 0;
            _lastRewardedShowTime = null;
        }
    }
}
