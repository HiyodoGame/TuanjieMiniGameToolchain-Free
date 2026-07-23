using System;
using UnityEngine;

namespace MiniGame.StarterKit.Runtime.WeChat
{
    /// <summary>
    /// 微信广告适配器。
    /// 封装激励视频、Banner、插屏广告的常用操作，并在非微信环境下提供 Editor Mock。
    /// 真实微信 SDK 调用通过 MGSWeChatSDKBridge 运行时反射完成，保证包体不强制依赖 com.qq.weixin.minigame。
    /// </summary>
    public static class MGSWeChatAdAdapter
    {
        /// <summary>
        /// 是否在 Editor 中模拟广告成功回调（仅用于本地 UI 流程测试）。
        /// </summary>
        public static bool SimulateSuccessInEditor = false;

        /// <summary>
        /// 激励视频关闭事件。参数：(success, result, errorMsg)。
        /// </summary>
        public static event Action<bool, MGSAdResult, string> OnRewardedVideoClosed;

        /// <summary>
        /// Banner 加载事件。参数：(success, errorMsg)。
        /// </summary>
        public static event Action<bool, string> OnBannerLoaded;

        private static object s_rewardedVideo;
        private static object s_banner;

        /// <summary>
        /// 展示激励视频广告。
        /// </summary>
        /// <param name="adUnitId">广告位 ID。</param>
        /// <param name="callback">单次回调。</param>
        public static void ShowRewardedVideoAd(string adUnitId, WXCallback<MGSAdResult> callback = null)
        {
            if (!MGSWeChatAdapter.IsWeChat)
            {
                if (SimulateSuccessInEditor)
                {
                    var mock = new MGSAdResult { Type = MGSAdType.RewardedVideo, IsEnded = true };
                    callback?.Invoke(true, mock, null);
                    OnRewardedVideoClosed?.Invoke(true, mock, null);
                    return;
                }

                const string err = "not_wechat";
                Debug.Log($"[MGSWeChatAdAdapter] RewardedVideo skipped in non-WeChat environment. ({err})");
                callback?.Invoke(false, null, err);
                OnRewardedVideoClosed?.Invoke(false, null, err);
                return;
            }

            if (!MGSWeChatSDKBridge.IsAvailable)
            {
                const string err = "wechat_sdk_not_linked";
                Debug.LogWarning($"[MGSWeChatAdAdapter] RewardedVideo real SDK not linked. ({err})");
                callback?.Invoke(false, null, err);
                OnRewardedVideoClosed?.Invoke(false, null, err);
                return;
            }

            DestroyRewardedVideo();

            s_rewardedVideo = MGSWeChatSDKBridge.CreateRewardedVideoAd(adUnitId);
            if (s_rewardedVideo == null)
            {
                const string err = "create_rewarded_video_failed";
                callback?.Invoke(false, null, err);
                OnRewardedVideoClosed?.Invoke(false, null, err);
                return;
            }

            MGSWeChatSDKBridge.RewardedVideoOnError(s_rewardedVideo, err =>
            {
                var msg = MGSWeChatSDKBridge.GetErrorMessage(err);
                callback?.Invoke(false, null, msg);
                OnRewardedVideoClosed?.Invoke(false, null, msg);
            });

            MGSWeChatSDKBridge.RewardedVideoOnClose(s_rewardedVideo, res =>
            {
                var result = new MGSAdResult { Type = MGSAdType.RewardedVideo, IsEnded = MGSWeChatSDKBridge.GetIsEnded(res) };
                callback?.Invoke(true, result, null);
                OnRewardedVideoClosed?.Invoke(true, result, null);
            });

            MGSWeChatSDKBridge.RewardedVideoLoad(
                s_rewardedVideo,
                _ => MGSWeChatSDKBridge.RewardedVideoShow(
                    s_rewardedVideo,
                    __ => { },
                    failed =>
                    {
                        var msg = MGSWeChatSDKBridge.GetErrorMessage(failed);
                        callback?.Invoke(false, null, msg);
                        OnRewardedVideoClosed?.Invoke(false, null, msg);
                    }),
                err =>
                {
                    var msg = MGSWeChatSDKBridge.GetErrorMessage(err);
                    callback?.Invoke(false, null, msg);
                    OnRewardedVideoClosed?.Invoke(false, null, msg);
                });
        }

        /// <summary>
        /// 在屏幕底部加载并展示 Banner 广告。
        /// </summary>
        /// <param name="adUnitId">广告位 ID。</param>
        /// <param name="width">广告宽度。</param>
        /// <param name="height">广告高度。</param>
        /// <param name="adIntervals">自动刷新间隔（秒），至少 30。</param>
        /// <param name="callback">单次加载回调。</param>
        public static void LoadBannerAd(string adUnitId, int width, int height, int adIntervals = 30, WXCallback<bool> callback = null)
        {
            if (!MGSWeChatAdapter.IsWeChat)
            {
                const string err = "not_wechat";
                Debug.Log($"[MGSWeChatAdAdapter] Banner skipped in non-WeChat environment. ({err})");
                callback?.Invoke(false, false, err);
                OnBannerLoaded?.Invoke(false, err);
                return;
            }

            if (!MGSWeChatSDKBridge.IsAvailable)
            {
                const string err = "wechat_sdk_not_linked";
                Debug.LogWarning($"[MGSWeChatAdAdapter] Banner real SDK not linked. ({err})");
                callback?.Invoke(false, false, err);
                OnBannerLoaded?.Invoke(false, err);
                return;
            }

            DestroyBanner();

            var windowHeight = MGSWeChatSDKBridge.GetWindowHeight();
            var top = (int)(windowHeight - height);
            s_banner = MGSWeChatSDKBridge.CreateBannerAd(adUnitId, width, height, top, adIntervals);
            if (s_banner == null)
            {
                const string err = "create_banner_failed";
                callback?.Invoke(false, false, err);
                OnBannerLoaded?.Invoke(false, err);
                return;
            }

            MGSWeChatSDKBridge.BannerOnError(s_banner, err =>
            {
                var msg = MGSWeChatSDKBridge.GetErrorMessage(err);
                callback?.Invoke(false, false, msg);
                OnBannerLoaded?.Invoke(false, msg);
            });

            MGSWeChatSDKBridge.BannerOnLoad(s_banner, _ =>
            {
                callback?.Invoke(true, true, null);
                OnBannerLoaded?.Invoke(true, null);
            });

            MGSWeChatSDKBridge.BannerShow(
                s_banner,
                _ => { },
                err =>
                {
                    var msg = MGSWeChatSDKBridge.GetErrorMessage(err);
                    callback?.Invoke(false, false, msg);
                    OnBannerLoaded?.Invoke(false, msg);
                });
        }

        /// <summary>
        /// 隐藏当前 Banner 广告。
        /// </summary>
        public static void HideBannerAd()
        {
            if (s_banner != null)
                MGSWeChatSDKBridge.BannerHide(s_banner);
        }

        /// <summary>
        /// 销毁当前 Banner 广告。
        /// </summary>
        public static void DestroyBannerAd()
        {
            DestroyBanner();
        }

        /// <summary>
        /// 销毁当前激励视频广告。
        /// </summary>
        public static void DestroyRewardedVideoAd()
        {
            DestroyRewardedVideo();
        }

        private static void DestroyRewardedVideo()
        {
            if (s_rewardedVideo == null) return;
            try { MGSWeChatSDKBridge.RewardedVideoDestroy(s_rewardedVideo); } catch (Exception ex) { Debug.LogWarning($"[MGSWeChatAdAdapter] 销毁激励视频广告异常: {ex.Message}"); }
            s_rewardedVideo = null;
        }

        private static void DestroyBanner()
        {
            if (s_banner == null) return;
            try { MGSWeChatSDKBridge.BannerDestroy(s_banner); } catch (Exception ex) { Debug.LogWarning($"[MGSWeChatAdAdapter] 销毁 Banner 广告异常: {ex.Message}"); }
            s_banner = null;
        }
    }
}
