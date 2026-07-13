#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MiniGame.Core.Editor.Licensing
{
    /// <summary>
    /// 在编辑器窗口顶部统一显示当前许可证等级与升级入口。
    /// </summary>
    public static class MiniGameLicenseStatusBar
    {
        /// <summary>
        /// 绘制许可证状态条。建议在各产品主窗口 OnGUI 开头调用。
        /// </summary>
        public static void Draw()
        {
            EditorGUILayout.BeginHorizontal("box");

            GUILayout.Label($"License: {MiniGameLicenseManager.CurrentTier}", EditorStyles.boldLabel);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("License Settings", GUILayout.Width(120f)))
            {
                MiniGameLicenseWindow.ShowWindow();
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif
