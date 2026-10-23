using MiniGame.StarterKit.Runtime.UI;
using MiniGame.StarterKit.Runtime.WeChat;
using UnityEngine;

namespace MiniGame.StarterKit.Runtime.Template
{
    /// <summary>
    /// UGUI 安全区适配器。
    /// 根据 Screen.safeArea 或微信 WX.GetWindowInfo().safeArea 自动调整 RectTransform 边距，
    /// 避免刘海屏、圆角屏、底部手势条遮挡 UI。
    /// 通常挂载在 Canvas 下的全屏根面板上。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class MGSUGUIFit : MonoBehaviour
    {
        [Tooltip("是否在 Awake 时自动应用")]
        public bool ApplyOnAwake = true;

        [Tooltip("是否在屏幕尺寸变化时自动重新应用（旋转、分屏等）")]
        public bool ApplyOnResolutionChange = true;

        [Tooltip("额外边距：X=Left, Y=Bottom, Z=Right, W=Top（像素）")]
        public Vector4 Padding;

        [Tooltip("是否将锚点重置为全拉伸（0,0)-(1,1)。若面板有自定义锚点需求请关闭")]
        public bool ResetAnchors = true;

        private RectTransform _rectTransform;
        private Vector2 _lastScreenSize;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            if (ApplyOnAwake)
                Apply();
        }

        private void Update()
        {
            if (!ApplyOnResolutionChange) return;

            var current = new Vector2(Screen.width, Screen.height);
            if (current != _lastScreenSize)
            {
                Apply();
                _lastScreenSize = current;
            }
        }

        /// <summary>
        /// 立即应用安全区适配。
        /// </summary>
        public void Apply()
        {
            if (_rectTransform == null) return;

            var safeArea = MGSUIHelper.GetSafeArea();
            MGSUIHelper.ApplySafeArea(_rectTransform, safeArea, Padding, ResetAnchors);
            _lastScreenSize = new Vector2(Screen.width, Screen.height);
        }
    }
}
