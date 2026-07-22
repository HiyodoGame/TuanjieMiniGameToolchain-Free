using System;
using UnityEngine;

namespace MiniGame.StarterKit.Runtime.WeChat
{
    /// <summary>
    /// 微信虚拟支付适配器。
    /// 封装米大师游戏币支付，并在非微信环境下提供 Editor Mock。
    /// 真实微信 SDK 调用通过 MGSWeChatSDKBridge 运行时反射完成，保证包体不强制依赖 com.qq.weixin.minigame。
    /// </summary>
    public static class MGSWeChatIAPAdapter
    {
        /// <summary>
        /// 米大师支付结果事件。参数：(success, errorMsg)。
        /// </summary>
        public static event Action<bool, string> OnMidasPaymentResult;

        /// <summary>
        /// 当前环境是否支持米大师支付。结果通过 callback 与 OnMidasPaymentResult 返回。
        /// </summary>
        public static void CheckIsSupportMidasPayment(WXCallback<bool> callback = null)
        {
            if (!MGSWeChatAdapter.IsWeChat)
            {
                const string err = "not_wechat";
                Debug.Log($"[MGSWeChatIAPAdapter] CheckIsSupportMidasPayment skipped in non-WeChat environment. ({err})");
                callback?.Invoke(false, false, err);
                OnMidasPaymentResult?.Invoke(false, err);
                return;
            }

            if (!MGSWeChatSDKBridge.IsAvailable)
            {
                const string err = "wechat_sdk_not_linked";
                Debug.LogWarning($"[MGSWeChatIAPAdapter] CheckIsSupportMidasPayment real SDK not linked. ({err})");
                callback?.Invoke(false, false, err);
                OnMidasPaymentResult?.Invoke(false, err);
                return;
            }

            MGSWeChatSDKBridge.CheckIsSupportMidasPayment(
                _ =>
                {
                    callback?.Invoke(true, true, null);
                    OnMidasPaymentResult?.Invoke(true, null);
                },
                err =>
                {
                    var msg = MGSWeChatSDKBridge.GetErrorMessage(err) ?? "check_failed";
                    callback?.Invoke(false, false, msg);
                    OnMidasPaymentResult?.Invoke(false, msg);
                });
        }

        /// <summary>
        /// 发起米大师游戏币支付。
        /// </summary>
        /// <param name="info">支付参数。</param>
        /// <param name="callback">单次回调。</param>
        public static void RequestMidasPayment(MGSWeChatPaymentInfo info, WXCallback<bool> callback = null)
        {
            if (info == null)
            {
                const string err = "payment_info_null";
                callback?.Invoke(false, false, err);
                OnMidasPaymentResult?.Invoke(false, err);
                return;
            }

            if (!MGSWeChatAdapter.IsWeChat)
            {
                const string err = "not_wechat";
                Debug.Log($"[MGSWeChatIAPAdapter] RequestMidasPayment skipped in non-WeChat environment. ({err})");
                callback?.Invoke(false, false, err);
                OnMidasPaymentResult?.Invoke(false, err);
                return;
            }

            if (!MGSWeChatSDKBridge.IsAvailable)
            {
                const string err = "wechat_sdk_not_linked";
                Debug.LogWarning($"[MGSWeChatIAPAdapter] RequestMidasPayment real SDK not linked. ({err})");
                callback?.Invoke(false, false, err);
                OnMidasPaymentResult?.Invoke(false, err);
                return;
            }

            // 实际接入前建议先调用 CheckIsSupportMidasPayment 确认环境支持
            MGSWeChatSDKBridge.RequestMidasPayment(
                info,
                _ =>
                {
                    callback?.Invoke(true, true, null);
                    OnMidasPaymentResult?.Invoke(true, null);
                },
                err =>
                {
                    var msg = MGSWeChatSDKBridge.GetErrorMessage(err) ?? "payment_failed";
                    callback?.Invoke(false, false, msg);
                    OnMidasPaymentResult?.Invoke(false, msg);
                });
        }
    }
}
