using UnityEngine;
using MiniGame.StarterKit.Runtime.WeChat;

namespace MiniGame.StarterKit.Runtime.UI
{
    /// <summary>
    /// UGUI 通用辅助工具。
    /// 提供安全区读取、全拉伸设置等常用方法。
    /// </summary>
    public static class MGSUIHelper
    {
        /// <summary>
        /// 获取当前运行环境的安全区。
        /// 微信环境下优先使用 WX.GetWindowInfo().safeArea，其他环境使用 Screen.safeArea。
        /// </summary>
        public static Rect GetSafeArea()
        {
            if (MGSWeChatAdapter.IsWeChat && MGSWeChatSDKBridge.TryGetSafeArea(out var safeArea))
            {
                return safeArea;
            }
            return Screen.safeArea;
        }

        /// <summary>
        /// 将 RectTransform 适配到指定安全区。
        /// </summary>
        /// <param name="rect">目标 RectTransform。</param>
        /// <param name="safeArea">安全区（屏幕坐标）。</param>
        /// <param name="padding">额外边距：X=Left, Y=Bottom, Z=Right, W=Top（像素）。</param>
        /// <param name="resetAnchors">是否重置为全拉伸锚点。</param>
        public static void ApplySafeArea(RectTransform rect, Rect safeArea, Vector4 padding, bool resetAnchors = true)
        {
            if (rect == null) return;

            var screen = new Vector2(Screen.width, Screen.height);
            if (screen.x <= 0 || screen.y <= 0) return;

            if (resetAnchors)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.pivot = new Vector2(0.5f, 0.5f);
            }

            float left = Mathf.Max(0f, safeArea.x + padding.x);
            float bottom = Mathf.Max(0f, safeArea.y + padding.y);
            float right = Mathf.Max(0f, screen.x - safeArea.xMax - padding.z);
            float top = Mathf.Max(0f, screen.y - safeArea.yMax - padding.w);

            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        /// <summary>
        /// 将 RectTransform 设置为全拉伸并清空 offset。
        /// </summary>
        public static void SetFullStretch(RectTransform rect)
        {
            if (rect == null) return;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
