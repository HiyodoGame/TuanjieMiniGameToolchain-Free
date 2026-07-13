#if UNITY_EDITOR
namespace MiniGame.Core.Editor.Licensing
{
    /// <summary>
    /// 许可证版本等级。
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
