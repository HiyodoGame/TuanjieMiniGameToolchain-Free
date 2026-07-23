using System;
using UnityEngine;

namespace MiniGame.StarterKit.Runtime.WeChat
{
    /// <summary>
    /// 微信小游戏适配器。
    /// 提供登录、分享、云存储等能力的统一入口，并在非微信环境下提供 Editor Mock。
    /// 真实微信 SDK 接入点已用 TODO 标出，接入后替换对应分支即可。
    /// </summary>
    public static class MGSWeChatAdapter
    {
        /// <summary>
        /// 是否在微信小游戏运行环境。Editor 与原生平台均返回 false。
        /// </summary>
        public static bool IsWeChat => Application.platform == RuntimePlatform.WebGLPlayer && !Application.isEditor;

        /// <summary>
        /// 登录结果事件。参数：(success, userInfo, errorMsg)。</summary>
        public static event Action<bool, WXUserInfo, string> OnLoginResult;

        /// <summary>
        /// 分享结果事件。参数：(success, errorMsg)。</summary>
        public static event Action<bool, string> OnShareResult;

        /// <summary>
        /// 云存储读取事件。参数：(success, value, errorMsg)。</summary>
        public static event Action<bool, string, string> OnCloudStorageRead;

        /// <summary>
        /// 云存储写入事件。参数：(success, errorMsg)。</summary>
        public static event Action<bool, string> OnCloudStorageWrite;

        /// <summary>
        /// 触发微信登录，结果通过 callback 与 OnLoginResult 返回。
        /// </summary>
        /// <param name="callback">可选的单次回调。</param>
        public static void Login(WXCallback<WXUserInfo> callback = null)
        {
            if (!IsWeChat)
            {
                const string err = "not_wechat";
                Debug.Log($"[MGSWeChatAdapter] Login skipped in non-WeChat environment. ({err})");
                callback?.Invoke(false, null, err);
                OnLoginResult?.Invoke(false, null, err);
                return;
            }

            // TODO: 接入真实微信 SDK：WX.Login(..., resp => {
            //     var user = new WXUserInfo { OpenId = resp.code };
            //     callback?.Invoke(true, user, null);
            //     OnLoginResult?.Invoke(true, user, null);
            // }, err => { ... });

            const string notImpl = "not_implemented";
            callback?.Invoke(false, null, notImpl);
            OnLoginResult?.Invoke(false, null, notImpl);
        }

        /// <summary>
        /// 触发微信分享（wx.shareAppMessage），结果通过 callback 与 OnShareResult 返回。
        /// </summary>
        /// <param name="info">分享内容。</param>
        /// <param name="callback">可选的单次回调。</param>
        public static void ShareAppMessage(WXShareInfo info, WXCallback<bool> callback = null)
        {
            if (!IsWeChat)
            {
                const string err = "not_wechat";
                Debug.Log($"[MGSWeChatAdapter] Share skipped in non-WeChat environment. ({err})");
                callback?.Invoke(false, false, err);
                OnShareResult?.Invoke(false, err);
                return;
            }

            // TODO: 接入真实微信 SDK：WX.ShareAppMessage(new WXShareAppMessageParam {
            //     title = info.Title,
            //     imageUrl = info.ImageUrl,
            //     query = info.Query
            // }, () => { callback?.Invoke(true, true, null); OnShareResult?.Invoke(true, null); });

            const string notImpl = "not_implemented";
            callback?.Invoke(false, false, notImpl);
            OnShareResult?.Invoke(false, notImpl);
        }

        /// <summary>
        /// 读取微信云存储（wx.getUserCloudStorage）。
        /// </summary>
        public static void GetCloudStorage(string key)
        {
            if (!IsWeChat)
            {
                const string err = "not_wechat";
                OnCloudStorageRead?.Invoke(false, null, err);
                return;
            }

            // TODO: 接入真实微信 SDK：WX.GetUserCloudStorage(keyList, resp => { ... });
            OnCloudStorageRead?.Invoke(false, null, "not_implemented");
        }

        /// <summary>
        /// 写入微信云存储（wx.setUserCloudStorage）。
        /// </summary>
        public static void SetCloudStorage(string key, string value)
        {
            if (!IsWeChat)
            {
                const string err = "not_wechat";
                OnCloudStorageWrite?.Invoke(false, err);
                return;
            }

            // TODO: 接入真实微信 SDK：WX.SetUserCloudStorage(kvList, resp => { ... });
            OnCloudStorageWrite?.Invoke(false, "not_implemented");
        }
    }
}
