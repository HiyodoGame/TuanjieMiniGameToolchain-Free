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
    /// Editor 中用于显式测试业务流程的微信 API 模拟结果。Disabled 为默认值，不会伪造成功。
    /// </summary>
    public enum MGSWeChatEditorMockResult
    {
        Disabled,
        Success,
        Cancelled,
        Failed
    }

    /// <summary>
    /// 微信登录结果。Code 是 wx.login 返回的临时登录凭证，必须交给业务服务器换取 openid/session_key，
    /// 不能当作用户标识持久化。
    /// </summary>
    [Serializable]
    public class WXLoginResult
    {
        public string Code;

        public override string ToString()
        {
            return string.IsNullOrEmpty(Code) ? "WXLoginResult[Code=<empty>]" : "WXLoginResult[Code=<received>]";
        }
    }

    /// <summary>
    /// 旧版占位用户模型。wx.login 不会返回这些字段；用户身份必须由业务服务器通过登录凭证换取。
    /// </summary>
    [Serializable]
    [Obsolete("wx.login 不返回用户资料。请使用 WXLoginResult，并在业务服务器用 Code 换取 openid/session_key。")]
    public class WXUserInfo
    {
        public string OpenId;
        public string NickName;
        public string AvatarUrl;
        public string UnionId;
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
