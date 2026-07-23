#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MiniGame.Core.Editor
{
    /// <summary>
    /// 品牌视觉统一工具：提供 Logo 加载、标题栏图标和窗口头部绘制。
    /// </summary>
    public static class MiniGameBranding
    {
        private const string LogoIconResource = "MiniGameToolchain/Logo";
        private const string LogoTextResource = "MiniGameToolchain/LogoText";

        private static Texture2D s_logoIcon;
        private static Texture2D s_logoText;

        /// <summary>
        /// 图标版 Logo（所有工具界面共用）。
        /// </summary>
        public static Texture2D LogoIcon
        {
            get
            {
                if (s_logoIcon == null)
                {
                    s_logoIcon = LoadTexture(LogoIconResource);
                }
                return s_logoIcon;
            }
        }

        /// <summary>
        /// 文字版 Logo（仅在欢迎页使用）。
        /// </summary>
        public static Texture2D LogoText
        {
            get
            {
                if (s_logoText == null)
                {
                    s_logoText = LoadTexture(LogoTextResource);
                }
                return s_logoText;
            }
        }

        /// <summary>
        /// 设置编辑器窗口标题栏的小图标。
        /// </summary>
        public static void SetTitleIcon(EditorWindow window, string title)
        {
            if (window == null) return;
            window.titleContent = new GUIContent(title, LogoIcon);
        }

        /// <summary>
        /// 在窗口顶部绘制统一的品牌头部。
        /// </summary>
        /// <param name="title">窗口标题</param>
        /// <param name="subtitle">副标题（可选）</param>
        /// <param name="showLogoText">是否同时显示 Logo 文字版（仅欢迎页使用）</param>
        /// <param name="showIcon">是否显示 Logo 图标版</param>
        /// <param name="iconSize">图标尺寸</param>
        public static void DrawHeader(
            string title,
            string subtitle = null,
            bool showLogoText = false,
            bool showIcon = true,
            int iconSize = 48)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (showIcon)
            {
                if (LogoIcon != null)
                {
                    GUILayout.Label(LogoIcon, GUILayout.Width(iconSize), GUILayout.Height(iconSize));
                }
                else
                {
                    GUILayout.Label(
                        "🎮",
                        new GUIStyle(GUI.skin.label)
                        {
                            fontSize = iconSize,
                            alignment = TextAnchor.MiddleCenter
                        },
                        GUILayout.Width(iconSize),
                        GUILayout.Height(iconSize)
                    );
                }
            }

            if (showLogoText && LogoText != null)
            {
                // 保持 LogoText 原始宽高比，避免被拉伸或裁剪
                float aspect = (float)LogoText.width / Mathf.Max(1, LogoText.height);
                float logoWidth = iconSize * aspect;
                GUILayout.Label(
                    LogoText,
                    new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter },
                    GUILayout.Width(logoWidth),
                    GUILayout.Height(iconSize)
                );
            }
            else
            {
                GUILayout.BeginVertical(GUILayout.Height(iconSize));
                GUILayout.FlexibleSpace();
                GUILayout.Label(
                    title,
                    new GUIStyle(GUI.skin.label)
                    {
                        fontSize = 20,
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleLeft,
                        normal = new GUIStyleState { textColor = Color.white }
                    }
                );
                if (!string.IsNullOrEmpty(subtitle))
                {
                    GUILayout.Label(
                        subtitle,
                        new GUIStyle(GUI.skin.label)
                        {
                            fontSize = 12,
                            alignment = TextAnchor.MiddleLeft,
                            normal = new GUIStyleState { textColor = new Color(0.75f, 0.75f, 0.75f) }
                        }
                    );
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(12);
        }

        private static Texture2D LoadTexture(string resourceName)
        {
            Texture2D texture = Resources.Load<Texture2D>(resourceName);
            if (texture == null)
            {
                texture = EditorGUIUtility.Load(resourceName) as Texture2D;
            }

            return texture;
        }
    }
}
#endif
