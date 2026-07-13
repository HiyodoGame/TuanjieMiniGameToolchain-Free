using System;
using UnityEngine;

namespace MiniGame.StarterKit.Runtime.WeChat
{
    /// <summary>
    /// 微信小游戏适配器。提供登录、分享、云存储等能力的统一入口与 Editor Mock。
    /// </summary>
    public static class MGSWeChatAdapter
    {
        /// <summary>
        /// 是否在微信小游戏运行环境。
        /// </summary>
        public static bool IsWeChat => Application.platform == RuntimePlatform.WebGLPlayer;

        /// <summary>
        /// 登录回调。
        /// </summary>
        public static Action<bool, string> OnLoginResult;

        /// <summary>
        /// 分享回调。
        /// </summary>
        public static Action<bool> OnShareResult;

        /// <summary>
        /// 云存储读取回调。
        /// </summary>
        public static Action<bool, string> OnCloudStorageRead;

        /// <summary>
        /// 云存储写入回调。
        /// </summary>
        public static Action<bool> OnCloudStorageWrite;

        /// <summary>
        /// 触发微信登录。非微信环境下立即回调失败。
        /// </summary>
        public static void Login()
        {
            if (!IsWeChat)
            {
                Debug.Log("[MGSWeChatAdapter] Login skipped in non-WeChat environment.");
                OnLoginResult?.Invoke(false, "not_wechat");
                return;
            }

            // 实际项目中通过 JSBridge 调用 wx.login
            OnLoginResult?.Invoke(false, "not_implemented");
        }

        /// <summary>
        /// 触发微信分享。
        /// </summary>
        public static void Share(string title, string imageUrl)
        {
            if (!IsWeChat)
            {
                Debug.Log("[MGSWeChatAdapter] Share skipped in non-WeChat environment.");
                OnShareResult?.Invoke(false);
                return;
            }

            // 实际项目中通过 JSBridge 调用 wx.shareAppMessage
            OnShareResult?.Invoke(false);
        }

        /// <summary>
        /// 读取微信云存储。
        /// </summary>
        public static void GetCloudStorage(string key)
        {
            if (!IsWeChat)
            {
                OnCloudStorageRead?.Invoke(false, null);
                return;
            }

            OnCloudStorageRead?.Invoke(false, null);
        }

        /// <summary>
        /// 写入微信云存储。
        /// </summary>
        public static void SetCloudStorage(string key, string value)
        {
            if (!IsWeChat)
            {
                OnCloudStorageWrite?.Invoke(false);
                return;
            }

            OnCloudStorageWrite?.Invoke(false);
        }
    }
}
