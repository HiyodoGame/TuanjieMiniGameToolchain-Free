using MiniGame.StarterKit.Runtime.WeChat;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.StarterKit.Runtime.Template
{
    /// <summary>
    /// 微信登录与分享示例控制器。
    /// 绑定登录/分享按钮和结果文本，演示如何调用 MGSWeChatAdapter。
    /// </summary>
    public class MGSLoginManager : MonoBehaviour
    {
        [Tooltip("登录按钮")]
        public Button LoginButton;

        [Tooltip("分享按钮")]
        public Button ShareButton;

        [Tooltip("结果显示文本")]
        public Text ResultText;

        [Tooltip("分享标题")]
        public string ShareTitle = "快来体验我的微信小游戏！";

        private void Start()
        {
            if (LoginButton != null)
                LoginButton.onClick.AddListener(OnLoginClick);

            if (ShareButton != null)
                ShareButton.onClick.AddListener(OnShareClick);
        }

        private void OnDestroy()
        {
            if (LoginButton != null)
                LoginButton.onClick.RemoveListener(OnLoginClick);

            if (ShareButton != null)
                ShareButton.onClick.RemoveListener(OnShareClick);
        }

        private void OnLoginClick()
        {
            ShowHint("正在登录...");
            MGSWeChatAdapter.Login(OnLoginResult);
        }

        private void OnShareClick()
        {
            ShowHint("正在唤起分享...");
            var info = new WXShareInfo(ShareTitle);
            MGSWeChatAdapter.ShareAppMessage(info, OnShareResult);
        }

        private void OnLoginResult(bool success, WXUserInfo user, string errorMsg)
        {
            if (ResultText == null) return;

            ResultText.text = success
                ? $"登录成功: {user}"
                : $"登录失败: {errorMsg}";
        }

        private void OnShareResult(bool success, bool data, string errorMsg)
        {
            if (ResultText == null) return;

            ResultText.text = success
                ? "分享成功"
                : $"分享失败: {errorMsg}";
        }

        private void ShowHint(string message)
        {
            if (ResultText != null)
                ResultText.text = message;
        }
    }
}
