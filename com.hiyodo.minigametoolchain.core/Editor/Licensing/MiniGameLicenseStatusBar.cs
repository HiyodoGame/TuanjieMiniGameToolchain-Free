#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MiniGame.Core.Editor.Licensing
{
    /// <summary>
    /// 在编辑器窗口顶部统一显示当前版本标识。不再提供许可证设置入口（已废弃秘钥系统）。
    /// </summary>
    public static class MiniGameLicenseStatusBar
    {
        /// <summary>
        /// 绘制版本状态条。建议在各产品主窗口 OnGUI 开头调用。
        /// </summary>
        public static void Draw()
        {
            EditorGUILayout.BeginHorizontal("box");

            var label = MiniGameLicenseManager.IsAssetStoreEdition
                ? "Builder Pro"
                : "Free Edition";
            GUILayout.Label(label, EditorStyles.boldLabel);

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif
