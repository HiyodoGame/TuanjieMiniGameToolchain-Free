#if UNITY_EDITOR
namespace MiniGame.Core.Editor
{
    /// <summary>
    /// 工具链版本常量。
    /// 该常量用于 .unitypackage 导入场景（UPM 的 package.json 不会随包导入），
    /// 确保欢迎页等 UI 能显示正确版本。
    /// 发布前请与 package.json 中的 version 字段保持一致。
    /// </summary>
    public static class MiniGameToolchainVersion
    {
        /// <summary>
        /// 当前工具链版本号。
        /// </summary>
        public const string CurrentVersion = "0.3.1";
    }
}
#endif
