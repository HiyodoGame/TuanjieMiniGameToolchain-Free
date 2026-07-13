using UnityEngine;

namespace MiniGame.StarterKit.Runtime.Template
{
    /// <summary>
    /// UGUI 安全区适配。根据 Screen.safeArea 自动调整 RectTransform 边距，
    /// 避免微信小游戏刘海屏、圆角屏遮挡 UI。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class MGSUGUIFit : MonoBehaviour
    {
        private RectTransform _rectTransform;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            ApplySafeArea();
        }

        private void ApplySafeArea()
        {
            if (_rectTransform == null) return;

            var safeArea = Screen.safeArea;
            var screenSize = new Vector2(Screen.width, Screen.height);

            _rectTransform.anchorMin = Vector2.zero;
            _rectTransform.anchorMax = Vector2.one;
            _rectTransform.pivot = new Vector2(0.5f, 0.5f);

            _rectTransform.offsetMin = new Vector2(safeArea.x, safeArea.y);
            _rectTransform.offsetMax = new Vector2(
                safeArea.x - screenSize.x,
                safeArea.y - screenSize.y);
        }
    }
}
