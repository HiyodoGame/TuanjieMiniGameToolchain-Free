using System;

namespace MiniGame.StarterKit.Runtime.WeChat
{
    /// <summary>
    /// 微信 API 统一回调委托。
    /// </summary>
    /// <typeparam name="T">成功时返回的数据类型。</typeparam>
    /// <param name="success">是否成功。</param>
    /// <param name="data">成功数据，失败时为 default。</param>
    /// <param name="errorMsg">失败时的错误信息。</param>
    public delegate void WXCallback<T>(bool success, T data, string errorMsg);

    /// <summary>
    /// 微信用户信息。
    /// </summary>
    [Serializable]
    public class WXUserInfo
    {
        public string OpenId;
        public string NickName;
        public string AvatarUrl;
        public string UnionId;

        public override string ToString()
        {
            return $"WXUserInfo[OpenId={OpenId}, NickName={NickName}]";
        }
    }

    /// <summary>
    /// 微信分享参数。
    /// </summary>
    [Serializable]
    public class WXShareInfo
    {
        public string Title;
        public string ImageUrl;
        public string Query;

        public WXShareInfo(string title, string imageUrl = null, string query = null)
        {
            Title = title;
            ImageUrl = imageUrl;
            Query = query;
        }
    }

    /// <summary>
    /// 广告类型。
    /// </summary>
    public enum MGSAdType
    {
        RewardedVideo,
        Banner,
        Interstitial,
        Custom
    }

    /// <summary>
    /// 广告展示结果。
    /// </summary>
    [Serializable]
    public class MGSAdResult
    {
        public MGSAdType Type;
        public bool IsEnded;

        public override string ToString()
        {
            return $"MGSAdResult[Type={Type}, IsEnded={IsEnded}]";
        }
    }

    /// <summary>
    /// 米大师支付信息。
    /// </summary>
    [Serializable]
    public class MGSWeChatPaymentInfo
    {
        public string OfferId;
        public string OutTradeNo;
        public int BuyQuantity = 1;
        public string CurrencyType = "CNY";
        public string Platform = "android";
        public int Env = 0;
        public string ZoneId = "1";
        public string Mode = "game";
    }
}
