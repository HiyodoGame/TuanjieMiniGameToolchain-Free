using MiniGame.StarterKit.Runtime.WeChat;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.StarterKit.Runtime.Template
{
    /// <summary>
    /// 微信登录示例控制器。绑定登录按钮和结果文本，演示如何调用 MGSWeChatAdapter。
    /// </summary>
    public class MGSLoginManager : MonoBehaviour
    {
        [Tooltip("登录按钮")]
        public Button LoginButton;

        [Tooltip("结果显示文本")]
        public Text ResultText;

        private void Start()
        {
            if (LoginButton != null)
            {
                LoginButton.onClick.AddListener(OnLoginClick);
            }

            MGSWeChatAdapter.OnLoginResult = OnLoginResult;
        }

        private void OnDestroy()
        {
            if (LoginButton != null)
            {
                LoginButton.onClick.RemoveListener(OnLoginClick);
            }
        }

        private void OnLoginClick()
        {
            if (ResultText != null)
            {
                ResultText.text = "正在登录...";
            }

            MGSWeChatAdapter.Login();
        }

        private void OnLoginResult(bool success, string message)
        {
            if (ResultText == null) return;

            ResultText.text = success
                ? $"登录成功: {message}"
                : $"登录失败: {message}";
        }
    }
}
