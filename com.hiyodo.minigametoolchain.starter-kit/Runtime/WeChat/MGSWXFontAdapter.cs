using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.StarterKit.Runtime.WeChat
{
    /// <summary>
    /// 微信字体适配器。
    /// 微信小游戏 wasm 环境的内置动态字体不含 CJK 字形，中文 UGUI 文本会渲染为空白；
    /// 本组件在 Start 时通过 WX.GetWXFont（经 MGSWeChatSDKBridge 反射调用）获取微信系统字体，
    /// 并应用到子层级所有 legacy Text。非微信环境或获取失败时保持原字体不变。
    /// 挂在 Canvas 或 UI 根节点上即可。
    /// </summary>
    public class MGSWXFontAdapter : MonoBehaviour
    {
        [Tooltip("是否在 Start 时自动获取并应用微信字体")]
        public bool ApplyOnStart = true;

        [Tooltip("微信系统字体不可用时回退下载的 ttf 地址（CDN 地址），可为空")]
        public string FallbackUrl = string.Empty;

        [Tooltip("是否包含未激活子物体上的 Text")]
        public bool IncludeInactive = true;

        private static Font _cachedFont;

        private void Start()
        {
            if (ApplyOnStart)
                Apply();
        }

        /// <summary>
        /// 立即获取微信字体并应用到子层级所有 Text。可在动态创建 UI 后再次调用。
        /// </summary>
        public void Apply()
        {
            if (!MGSWeChatAdapter.IsWeChat) return;

            if (_cachedFont != null)
            {
                ApplyFont(_cachedFont);
                return;
            }

            var ok = MGSWeChatSDKBridge.TryGetWXFont(FallbackUrl, font =>
            {
                if (font == null)
                {
                    Debug.LogWarning("[MGSWXFontAdapter] 获取微信字体失败，保持默认字体（中文可能无法显示）。");
                    return;
                }
                _cachedFont = font;
                ApplyFont(font);
            });

            if (!ok)
                Debug.LogWarning("[MGSWXFontAdapter] 微信 SDK 字体接口不可用，保持默认字体。");
        }

        private void ApplyFont(Font font)
        {
            foreach (var text in GetComponentsInChildren<Text>(IncludeInactive))
            {
                if (text != null)
                    text.font = font;
            }
        }
    }
}
