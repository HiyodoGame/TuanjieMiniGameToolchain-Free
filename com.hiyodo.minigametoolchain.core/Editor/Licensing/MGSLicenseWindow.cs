#if UNITY_EDITOR
using MiniGame.Core.Editor;
using MiniGame.Core.Editor.Licensing;
using UnityEditor;
using UnityEngine;

namespace MiniGame.Core.Editor.Windows
{
    /// <summary>
    /// 许可证激活窗口。
    /// </summary>
    public class MGSLicenseWindow : EditorWindow
    {
        private string _licenseKey = string.Empty;
        private string _expiresAt = string.Empty;
        private string _statusMessage = string.Empty;
        private MessageType _statusType = MessageType.Info;

        [MenuItem("Window/MiniGame/License Manager")]
        public static void ShowWindow()
        {
            var window = GetWindow<MGSLicenseWindow>("License Manager");
            window.minSize = new Vector2(400, 300);
            MiniGameBranding.SetTitleIcon(window, "License");
            window.Show();
        }

        private void OnEnable()
        {
            RefreshStatus();
        }

        private void RefreshStatus()
        {
            var license = MGSLicenseActivator.GetCurrentLicense();
            if (license == null)
            {
                _statusMessage = "当前未激活任何许可证。";
                _statusType = MessageType.Info;
            }
            else if (MGSLicenseActivator.ValidateCurrentLicense(out var error))
            {
                _statusMessage = $"许可证有效：{license.Tier}\n设备：{license.DeviceFingerprint.Substring(0, 8)}...\n激活时间：{license.ActivatedAt}\n过期时间：{license.ExpiresAt ?? "永久"}";
                _statusType = MessageType.Info;
            }
            else
            {
                _statusMessage = $"许可证无效：{error}";
                _statusType = MessageType.Error;
            }
        }

        private void OnGUI()
        {
            MiniGameBranding.DrawHeader("License Manager", "MiniGame Toolchain 许可证管理");
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("当前状态", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(_statusMessage, _statusType);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"当前等级：{MiniGameLicenseManager.CurrentTier}", EditorStyles.boldLabel);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("激活新许可证", EditorStyles.boldLabel);
            _licenseKey = EditorGUILayout.TextField("License Key", _licenseKey);
            _expiresAt = EditorGUILayout.TextField("Expires At (optional)", _expiresAt);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Activate", GUILayout.Height(28)))
            {
                if (MGSLicenseActivator.Activate(_licenseKey, string.IsNullOrWhiteSpace(_expiresAt) ? null : _expiresAt, out var error))
                {
                    _statusMessage = "激活成功！";
                    _statusType = MessageType.Info;
                    _licenseKey = string.Empty;
                    _expiresAt = string.Empty;
                }
                else
                {
                    _statusMessage = $"激活失败：{error}";
                    _statusType = MessageType.Error;
                }
                RefreshStatus();
            }

            if (GUILayout.Button("Deactivate", GUILayout.Height(28)))
            {
                if (EditorUtility.DisplayDialog("确认移除", "确定要移除当前许可证吗？", "移除", "取消"))
                {
                    MGSLicenseActivator.Deactivate();
                    RefreshStatus();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("设备指纹", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(MGSLicenseActivator.GetDeviceFingerprint(), EditorStyles.miniLabel, GUILayout.Height(20f));

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "许可证密钥格式：MGBT-TTTT-XXXX-XXXX-XXXX-XXXX\n" +
                "TTTT：0000=Free, 0001=Personal, 0002=Professional, 0003=Team, 0004=Enterprise\n" +
                "过期时间格式：yyyy-MM-dd（留空表示永久）",
                MessageType.None);
        }
    }
}
#endif
