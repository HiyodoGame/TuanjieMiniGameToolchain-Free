using System;
using UnityEngine;

namespace MiniGame.StarterKit.Runtime.WeChat
{
    /// <summary>
    /// 微信小游戏适配器。
    /// 提供登录、分享、开放数据写入等能力的统一入口，并在非微信环境下安全失败。
    /// 真实微信 SDK 调用通过运行时反射完成，避免对 com.qq.weixin.minigame 形成编译期硬依赖。
    /// </summary>
    public static class MGSWeChatAdapter
    {
        /// <summary>
        /// Editor 模拟结果。默认 Disabled；仅在 Application.isEditor 时生效，便于测试成功、取消和失败 UI。
        /// </summary>
        public static MGSWeChatEditorMockResult EditorMockResult = MGSWeChatEditorMockResult.Disabled;

        /// <summary>
        /// 是否在微信小游戏运行环境。Editor 与原生平台均返回 false。
        /// 注意：团结引擎的微信小游戏运行时 platform 是 WeixinMiniGamePlayer（Tuanjie 扩展枚举，非 WebGLPlayer）。
        /// </summary>
        public static bool IsWeChat =>
            (Application.platform == RuntimePlatform.WebGLPlayer
#if TUANJIE_1_9_OR_NEWER
                || Application.platform == RuntimePlatform.WeixinMiniGamePlayer
#endif
            ) && !Application.isEditor;

        /// <summary>
        /// 登录结果事件。参数：(success, loginResult, errorMsg)。</summary>
        public static event Action<bool, WXLoginResult, string> OnLoginResult;

        /// <summary>
        /// 分享调用结果事件。success 仅表示已成功调用微信分享 API，不表示用户完成分享。</summary>
        public static event Action<bool, string> OnShareResult;

        /// <summary>
        /// 旧版云存储读取事件。主域无法读取用户托管数据，调用时固定返回 open_data_context_only。</summary>
        [Obsolete("wx.getUserCloudStorage 仅可在微信开放数据域调用；Starter Kit 主域适配器不提供读取能力。")]
        public static event Action<bool, string, string> OnCloudStorageRead;

        /// <summary>
        /// 云存储写入事件。参数：(success, errorMsg)。</summary>
        public static event Action<bool, string> OnCloudStorageWrite;

        /// <summary>
        /// 触发微信登录，结果通过 callback 与 OnLoginResult 返回。
        /// </summary>
        /// <param name="callback">可选的单次回调。</param>
        public static void Login(WXCallback<WXLoginResult> callback = null)
        {
            if (TryCompleteEditorLoginMock(callback)) return;

            if (!IsWeChat)
            {
                const string err = "not_wechat";
                Debug.Log($"[MGSWeChatAdapter] Login skipped in non-WeChat environment. ({err})");
                callback?.Invoke(false, null, err);
                OnLoginResult?.Invoke(false, null, err);
                return;
            }

            if (!MGSWeChatSDKBridge.IsAvailable)
            {
                CompleteLogin(false, null, "wechat_sdk_not_linked", callback);
                return;
            }

            var invoked = MGSWeChatSDKBridge.TryLogin(
                response =>
                {
                    var code = MGSWeChatSDKBridge.GetLoginCode(response);
                    if (string.IsNullOrEmpty(code))
                    {
                        CompleteLogin(false, null, "login_code_empty", callback);
                        return;
                    }

                    CompleteLogin(true, new WXLoginResult { Code = code }, null, callback);
                },
                error => CompleteLogin(false, null,
                    MGSWeChatSDKBridge.GetErrorMessage(error) ?? "login_failed", callback));

            if (!invoked)
                CompleteLogin(false, null, "wechat_login_api_unavailable", callback);
        }

        /// <summary>
        /// 触发微信分享（wx.shareAppMessage），结果通过 callback 与 OnShareResult 返回。
        /// </summary>
        /// <param name="info">分享内容。</param>
        /// <param name="callback">可选的单次回调。</param>
        public static void ShareAppMessage(WXShareInfo info, WXCallback<bool> callback = null)
        {
            if (info == null || string.IsNullOrWhiteSpace(info.Title))
            {
                const string invalidError = "share_info_invalid";
                callback?.Invoke(false, false, invalidError);
                OnShareResult?.Invoke(false, invalidError);
                return;
            }

            if (TryCompleteEditorShareMock(callback)) return;

            if (!IsWeChat)
            {
                const string err = "not_wechat";
                Debug.Log($"[MGSWeChatAdapter] Share skipped in non-WeChat environment. ({err})");
                callback?.Invoke(false, false, err);
                OnShareResult?.Invoke(false, err);
                return;
            }

            var invoked = MGSWeChatSDKBridge.IsAvailable && MGSWeChatSDKBridge.TryShareAppMessage(info);
            var error = invoked ? null : "wechat_share_api_unavailable";
            callback?.Invoke(invoked, invoked, error);
            OnShareResult?.Invoke(invoked, error);
        }

        /// <summary>
        /// 主域不支持读取微信用户托管数据。请在开放数据域中调用 wx.getUserCloudStorage。
        /// </summary>
        [Obsolete("wx.getUserCloudStorage 仅可在微信开放数据域调用。该方法只保留兼容性并固定返回失败。")]
        public static void GetCloudStorage(string key)
        {
            const string err = "open_data_context_only";
            Debug.LogWarning("[MGSWeChatAdapter] wx.getUserCloudStorage 仅可在微信开放数据域调用。");
            OnCloudStorageRead?.Invoke(false, null, err);
        }

        /// <summary>
        /// 写入微信云存储（wx.setUserCloudStorage）。
        /// </summary>
        public static void SetCloudStorage(string key, string value)
        {
            if (string.IsNullOrEmpty(key) || value == null)
            {
                OnCloudStorageWrite?.Invoke(false, "cloud_storage_data_invalid");
                return;
            }

            if (TryCompleteEditorCloudStorageMock()) return;

            if (!IsWeChat)
            {
                const string err = "not_wechat";
                OnCloudStorageWrite?.Invoke(false, err);
                return;
            }

            var invoked = MGSWeChatSDKBridge.IsAvailable && MGSWeChatSDKBridge.TrySetUserCloudStorage(
                key,
                value,
                _ => OnCloudStorageWrite?.Invoke(true, null),
                error => OnCloudStorageWrite?.Invoke(false,
                    MGSWeChatSDKBridge.GetErrorMessage(error) ?? "cloud_storage_write_failed"));

            if (!invoked)
                OnCloudStorageWrite?.Invoke(false, "wechat_cloud_storage_api_unavailable");
        }

        private static void CompleteLogin(bool success, WXLoginResult result, string error,
            WXCallback<WXLoginResult> callback)
        {
            callback?.Invoke(success, result, error);
            OnLoginResult?.Invoke(success, result, error);
        }

        private static bool TryCompleteEditorLoginMock(WXCallback<WXLoginResult> callback)
        {
            if (!Application.isEditor || EditorMockResult == MGSWeChatEditorMockResult.Disabled) return false;

            switch (EditorMockResult)
            {
                case MGSWeChatEditorMockResult.Success:
                    CompleteLogin(true, new WXLoginResult { Code = "editor_mock_login_code" }, null, callback);
                    break;
                case MGSWeChatEditorMockResult.Cancelled:
                    CompleteLogin(false, null, "editor_mock_cancelled", callback);
                    break;
                default:
                    CompleteLogin(false, null, "editor_mock_failed", callback);
                    break;
            }
            return true;
        }

        private static bool TryCompleteEditorShareMock(WXCallback<bool> callback)
        {
            if (!Application.isEditor || EditorMockResult == MGSWeChatEditorMockResult.Disabled) return false;

            var succeeded = EditorMockResult == MGSWeChatEditorMockResult.Success;
            var error = succeeded
                ? null
                : EditorMockResult == MGSWeChatEditorMockResult.Cancelled
                    ? "editor_mock_cancelled"
                    : "editor_mock_failed";
            callback?.Invoke(succeeded, succeeded, error);
            OnShareResult?.Invoke(succeeded, error);
            return true;
        }

        private static bool TryCompleteEditorCloudStorageMock()
        {
            if (!Application.isEditor || EditorMockResult == MGSWeChatEditorMockResult.Disabled) return false;

            var succeeded = EditorMockResult == MGSWeChatEditorMockResult.Success;
            var error = succeeded
                ? null
                : EditorMockResult == MGSWeChatEditorMockResult.Cancelled
                    ? "editor_mock_cancelled"
                    : "editor_mock_failed";
            OnCloudStorageWrite?.Invoke(succeeded, error);
            return true;
        }
    }
}
