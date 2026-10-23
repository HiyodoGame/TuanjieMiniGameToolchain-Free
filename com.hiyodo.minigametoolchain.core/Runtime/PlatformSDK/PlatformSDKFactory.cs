namespace MiniGame.Core.Runtime.PlatformSDK
{
    /// <summary>
    /// 平台 SDK 工厂：根据当前构建目标自动选择真实实现或 Mock 实现。
    /// </summary>
    public static class PlatformSDKFactory
    {
        /// <summary>
        /// 创建当前平台对应的 SDK 实例。
        /// </summary>
        public static IPlatformSDK Create()
        {
#if WEIXINMINIGAME && !UNITY_EDITOR
            return new WeChatSDKRuntime();
#else
            return new PlatformSDKMock();
#endif
        }
    }
}
