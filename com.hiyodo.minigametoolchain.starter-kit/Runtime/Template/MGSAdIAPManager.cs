using System;
using MiniGame.StarterKit.Runtime.WeChat;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.StarterKit.Runtime.Template
{
    /// <summary>
    /// 微信广告与支付示例控制器。
    /// 演示如何调用 MGSWeChatAdAdapter 播放激励视频，以及调用 MGSWeChatIAPAdapter 发起米大师支付。
    /// </summary>
    public class MGSAdIAPManager : MonoBehaviour
    {
        [Tooltip("看广告按钮")]
        public Button WatchAdButton;

        [Tooltip("支付按钮")]
        public Button PayButton;

        [Tooltip("结果显示文本")]
        public Text ResultText;

        [Tooltip("激励视频广告位 ID（需替换为正式 ID）")]
        public string RewardedAdUnitId = "adunit-xxxxxx";

        [Tooltip("米大师 offerId（需替换为正式 ID）")]
        public string OfferId = "123456";

        [Tooltip("购买数量")]
        public int BuyQuantity = 10;

        private void Start()
        {
            if (WatchAdButton != null)
                WatchAdButton.onClick.AddListener(OnWatchAdClick);

            if (PayButton != null)
                PayButton.onClick.AddListener(OnPayClick);
        }

        private void OnDestroy()
        {
            if (WatchAdButton != null)
                WatchAdButton.onClick.RemoveListener(OnWatchAdClick);

            if (PayButton != null)
                PayButton.onClick.RemoveListener(OnPayClick);
        }

        private void OnWatchAdClick()
        {
            ShowHint("正在加载激励视频...");
            MGSWeChatAdAdapter.ShowRewardedVideoAd(RewardedAdUnitId, OnAdResult);
        }

        private void OnPayClick()
        {
            ShowHint("正在请求支付...");
            var info = new MGSWeChatPaymentInfo
            {
                OfferId = OfferId,
                BuyQuantity = BuyQuantity,
                OutTradeNo = DateTime.UtcNow.Ticks.ToString()
            };
            MGSWeChatIAPAdapter.RequestMidasPayment(info, OnPayResult);
        }

        private void OnAdResult(bool success, MGSAdResult result, string errorMsg)
        {
            if (ResultText == null) return;

            if (success && result != null && result.IsEnded)
            {
                ResultText.text = $"广告播放完成，发放奖励: {result}";
            }
            else if (success)
            {
                ResultText.text = $"广告关闭，未完整播放: {result}";
            }
            else
            {
                ResultText.text = $"广告失败: {errorMsg}";
            }
        }

        private void OnPayResult(bool success, bool data, string errorMsg)
        {
            if (ResultText == null) return;

            ResultText.text = success
                ? "支付成功"
                : $"支付失败: {errorMsg}";
        }

        private void ShowHint(string message)
        {
            if (ResultText != null)
                ResultText.text = message;
        }
    }
}
