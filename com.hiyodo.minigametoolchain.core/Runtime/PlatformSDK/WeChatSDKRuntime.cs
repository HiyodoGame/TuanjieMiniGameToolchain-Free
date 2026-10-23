using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace MiniGame.Core.Runtime.PlatformSDK
{
    /// <summary>
    /// 微信小游戏平台 SDK 真机实现。
    /// 通过 DllImport 调用微信 JS-SDK 暴露的函数；Editor 下自动回退到 Mock。
    /// </summary>
    public class WeChatSDKRuntime : IPlatformSDK
    {
        #region Native JS Bridge

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void WXLogin(string callbackId);

        [DllImport("__Internal")]
        private static extern void WXShare(string jsonConfig, string callbackId);

        [DllImport("__Internal")]
        private static extern void WXShowRewardedAd(string adUnitId, string callbackId);

        [DllImport("__Internal")]
        private static extern void WXShowInterstitialAd(string adUnitId, string callbackId);

        [DllImport("__Internal")]
        private static extern void WXShowBannerAd(string adUnitId, int position);

        [DllImport("__Internal")]
        private static extern void WXHideBannerAd();

        [DllImport("__Internal")]
        private static extern void WXRequestPay(string jsonConfig, string callbackId);

        [DllImport("__Internal")]
        private static extern void WXSetRankData(string key, int score, string callbackId);

        [DllImport("__Internal")]
        private static extern void WXGetRankData(string key, string callbackId);

        [DllImport("__Internal")]
        private static extern void WXSaveToCloud(string key, string data, string callbackId);

        [DllImport("__Internal")]
        private static extern void WXLoadFromCloud(string key, string callbackId);

        [DllImport("__Internal")]
        private static extern void WXGetSystemInfo(string callbackId);
#endif

        #endregion

        private static readonly MockFallback _fallback = new MockFallback();

        public void Login(Action<LoginResult> callback)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var id = CallbackRegistry.Register(callback);
            WXLogin(id);
#else
            _fallback.Login(callback);
#endif
        }

        public void Share(ShareConfig config, Action<ShareResult> callback)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var id = CallbackRegistry.Register(callback);
            WXShare(JsonUtility.ToJson(config ?? new ShareConfig()), id);
#else
            _fallback.Share(config, callback);
#endif
        }

        public void ShowRewardedAd(string adUnitId, Action<AdResult> callback)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var id = CallbackRegistry.Register(callback);
            WXShowRewardedAd(adUnitId, id);
#else
            _fallback.ShowRewardedAd(adUnitId, callback);
#endif
        }

        public void ShowInterstitialAd(string adUnitId, Action<AdResult> callback)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var id = CallbackRegistry.Register(callback);
            WXShowInterstitialAd(adUnitId, id);
#else
            _fallback.ShowInterstitialAd(adUnitId, callback);
#endif
        }

        public void ShowBannerAd(string adUnitId, BannerPosition position)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            WXShowBannerAd(adUnitId, (int)position);
#else
            _fallback.ShowBannerAd(adUnitId, position);
#endif
        }

        public void HideBannerAd()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            WXHideBannerAd();
#else
            _fallback.HideBannerAd();
#endif
        }

        public void RequestPay(PayConfig config, Action<PayResult> callback)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var id = CallbackRegistry.Register(callback);
            WXRequestPay(JsonUtility.ToJson(config ?? new PayConfig()), id);
#else
            _fallback.RequestPay(config, callback);
#endif
        }

        public void SetRankData(string key, int score, Action<bool> callback)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var id = CallbackRegistry.Register(callback);
            WXSetRankData(key, score, id);
#else
            _fallback.SetRankData(key, score, callback);
#endif
        }

        public void GetRankData(string key, Action<RankData> callback)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var id = CallbackRegistry.Register(callback);
            WXGetRankData(key, id);
#else
            _fallback.GetRankData(key, callback);
#endif
        }

        public void SaveToCloud(string key, string data, Action<bool> callback)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var id = CallbackRegistry.Register(callback);
            WXSaveToCloud(key, data, id);
#else
            _fallback.SaveToCloud(key, data, callback);
#endif
        }

        public void LoadFromCloud(string key, Action<string> callback)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var id = CallbackRegistry.Register(callback);
            WXLoadFromCloud(key, id);
#else
            _fallback.LoadFromCloud(key, callback);
#endif
        }

        public void GetSystemInfo(Action<SystemInfoResult> callback)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var id = CallbackRegistry.Register(callback);
            WXGetSystemInfo(id);
#else
            _fallback.GetSystemInfo(callback);
#endif
        }

        /// <summary>
        /// 供 JS 侧调用的统一回调入口。
        /// </summary>
        public static void OnNativeCallback(string callbackId, string json)
        {
            CallbackRegistry.Invoke(callbackId, json);
        }

        /// <summary>
        /// 简单的回调注册表：将 C# 回调委托映射为字符串 callbackId。
        /// </summary>
        private static class CallbackRegistry
        {
            private static int _nextId;
            private static readonly Dictionary<string, Delegate> _callbacks = new Dictionary<string, Delegate>();

            public static string Register<T>(Action<T> callback)
            {
                var id = System.Threading.Interlocked.Increment(ref _nextId).ToString();
                _callbacks[id] = callback;
                return id;
            }

            public static void Invoke(string callbackId, string json)
            {
                if (!_callbacks.TryGetValue(callbackId, out var callback)) return;
                _callbacks.Remove(callbackId);

                try
                {
                    var type = callback.GetType().GetGenericArguments()[0];
                    var result = type == typeof(string)
                        ? json
                        : JsonUtility.FromJson(json, type);
                    callback.DynamicInvoke(result);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[WeChatSDKRuntime] Callback invoke failed: {ex}");
                }
            }
        }

        private class MockFallback : PlatformSDKMock { }
    }
}
