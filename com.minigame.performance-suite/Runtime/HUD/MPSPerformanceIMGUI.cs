using MiniGame.PerformanceSuite.Runtime.Data;
using UnityEngine;

namespace MiniGame.PerformanceSuite.Runtime.HUD
{
    /// <summary>
    /// IMGUI 降级面板渲染器。当 uGUI 不可用时由 MPSPerformanceHUD 自动启用。
    /// </summary>
    public class MPSPerformanceIMGUI : MonoBehaviour
    {
        [SerializeField]
        private MPSPerformanceConfig _config;

        [SerializeField]
        private MPSPerformanceHUD _hud;

        private Rect _windowRect;
        private GUIStyle _boxStyle;
        private GUIStyle _warningStyle;
        private GUIStyle _normalStyle;
        private bool _stylesInitialized;
        private float _lastStyleScale = -1f;
        private float _lastWindowScale = -1f;

        public bool IsVisible { get; set; } = true;

        public void Setup(MPSPerformanceConfig config, MPSPerformanceHUD hud)
        {
            _config = config;
            _hud = hud;
            ApplyWindowRect();
        }

        private void Start()
        {
            ApplyWindowRect();
        }

        private void ApplyWindowRect()
        {
            if (_config == null) return;

            float width = 240f * _config.Scale;
            float height = 140f * _config.Scale;
            float x = Screen.width * _config.DefaultAnchor.x;
            float y = Screen.height * (1f - _config.DefaultAnchor.y);
            _windowRect = new Rect(x, y, width, height);
        }

        private void OnGUI()
        {
            if (!IsVisible || _hud == null || _config == null)
            {
                return;
            }

            ApplyScaleToWindow();
            EnsureStyles();

            _windowRect = GUILayout.Window(
                0,
                _windowRect,
                DrawWindow,
                "MPS Performance",
                _boxStyle,
                GUILayout.Width(_windowRect.width),
                GUILayout.Height(_windowRect.height)
            );
        }

        private void DrawWindow(int windowId)
        {
            var current = _hud.CurrentSample;

            GUILayout.BeginVertical();

            DrawMetric("FPS", $"{current.Fps:F1}", current.Fps < _config.FpsWarningThreshold);
            DrawMetric("Unity Heap", FormatBytes(current.UnityHeapBytes), current.UnityHeapBytes > _config.UnityHeapWarningThresholdMB * 1024 * 1024);

            if (current.JsHeapBytes > 0)
            {
                DrawMetric("JS Heap", FormatBytes(current.JsHeapBytes), current.JsHeapBytes > _config.JsHeapWarningThresholdMB * 1024 * 1024);
            }

            DrawMetric("DrawCall", $"{current.DrawCalls}", false);
            DrawMetric("Triangles", FormatCount(current.TriangleCount), false);
            DrawMetric("GC Alloc", FormatBytes(current.GcAllocBytes), current.GcAllocBytes > _config.GcAllocWarningThresholdKB * 1024);

            GUILayout.EndVertical();

            if (_config.Draggable)
            {
                GUI.DragWindow();
            }
        }

        private void DrawMetric(string label, string value, bool warning)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, _normalStyle, GUILayout.Width(90f * _config.Scale));
            GUILayout.FlexibleSpace();
            GUILayout.Label(value, warning ? _warningStyle : _normalStyle);
            GUILayout.EndHorizontal();
        }

        private void EnsureStyles()
        {
            if (_stylesInitialized && Mathf.Abs(_config.Scale - _lastStyleScale) < 0.001f) return;

            _lastStyleScale = _config.Scale;
            _stylesInitialized = true;

            _boxStyle = new GUIStyle(GUI.skin.window)
            {
                fontSize = Mathf.RoundToInt(12 * _config.Scale)
            };

            _normalStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(12 * _config.Scale),
                normal = { textColor = Color.white }
            };

            _warningStyle = new GUIStyle(_normalStyle)
            {
                normal = { textColor = Color.red }
            };
        }

        private void ApplyScaleToWindow()
        {
            if (_config == null) return;
            if (Mathf.Abs(_config.Scale - _lastWindowScale) < 0.001f) return;

            _lastWindowScale = _config.Scale;
            float width = 240f * _config.Scale;
            float height = 140f * _config.Scale;
            _windowRect = new Rect(_windowRect.x, _windowRect.y, width, height);
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024f:F1} KB";
            return $"{bytes / (1024f * 1024f):F1} MB";
        }

        private static string FormatCount(int count)
        {
            if (count < 1000) return count.ToString();
            if (count < 1000000) return $"{count / 1000f:F1} K";
            return $"{count / 1000000f:F1} M";
        }
    }
}
