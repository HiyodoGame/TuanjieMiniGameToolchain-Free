#if UNITY_EDITOR
namespace MiniGame.Core.Editor.Licensing
{
    /// <summary>
    /// 许可证版本等级。
    /// 当前采用二态模型：Free（免费裁剪版）与 Enterprise（完整版 / AssetStore 付费版）。
    /// Personal、Professional、Team 为旧秘钥系统遗留值，已废弃但保留以避免序列化/二进制兼容性问题。
    /// </summary>
    public enum MiniGameLicenseTier
    {
        Free = 0,
        Personal = 1,
        Professional = 2,
        Team = 3,
        Enterprise = 4
    }
}
#endif
