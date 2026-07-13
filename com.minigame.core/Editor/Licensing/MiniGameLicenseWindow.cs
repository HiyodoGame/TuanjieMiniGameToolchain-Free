#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MiniGame.Core.Editor.Licensing
{
    /// <summary>
    /// 许可证设置窗口（主要用于本地评估、测试和演示）。
    /// </summary>
    public class MiniGameLicenseWindow : EditorWindow
    {
        private MiniGameLicenseTier _selectedTier;

        [MenuItem("Window/MiniGame/License Settings")]
        public static void ShowWindow()
        {
            var window = GetWindow<MiniGameLicenseWindow>("MiniGame License");
            window.minSize = new Vector2(360, 160);
            window.Show();
        }

        private void OnEnable()
        {
            _selectedTier = MiniGameLicenseManager.CurrentTier;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("MiniGame Toolchain License", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField($"当前许可证: {MiniGameLicenseManager.CurrentTier}", EditorStyles.largeLabel);
            EditorGUILayout.Space();

            _selectedTier = (MiniGameLicenseTier)EditorGUILayout.EnumPopup("切换许可证", _selectedTier);

            if (GUILayout.Button("应用", GUILayout.Height(28)))
            {
                MiniGameLicenseManager.CurrentTier = _selectedTier;
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("商业版本可接入授权服务器、本地加密文件或 AssetStore 收据校验。", MessageType.Info);
        }
    }
}
#endif
