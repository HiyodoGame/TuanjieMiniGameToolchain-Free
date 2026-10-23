using MiniGame.PerformanceSuite.Runtime.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MiniGame.PerformanceSuite.Runtime.HUD
{
    /// <summary>
    /// uGUI 性能面板渲染器。
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(GraphicRaycaster))]
    public class MPSPerformanceUI : MonoBehaviour
    {
        [SerializeField]
        private MPSPerformanceConfig _config;

        [SerializeField]
        private MPSPerformanceHUD _hud;

        [Header("UI 元素")]
        [SerializeField]
        private RectTransform _panelRoot;

        [SerializeField]
        private Text _fpsText;

        [SerializeField]
        private Text _unityHeapText;

        [SerializeField]
        private Text _jsHeapText;

        [SerializeField]
        private Text _drawCallText;

        [SerializeField]
        private Text _triangleText;

        [SerializeField]
        private Text _gcAllocText;

        [SerializeField]
        private Text _startupText;

        [SerializeField]
        private MPSPerformanceGraph _graph;

        private Canvas _canvas;
        private CanvasScaler _scaler;
        private RectTransform _rectTransform;
        private Image _panelImage;
        private Color _panelBaseColor;
        private float _lastAppliedScale = -1f;
        private Vector2 _dragOffset;

        public bool IsVisible
        {
            get => _canvas != null && _canvas.enabled;
            set
            {
                if (_canvas != null)
                {
                    _canvas.enabled = value;
                }
            }
        }

        public void Setup(MPSPerformanceConfig config, MPSPerformanceHUD hud)
        {
            _config = config;
            _hud = hud;
        }

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            _scaler = GetComponent<CanvasScaler>();
            _rectTransform = GetComponent<RectTransform>();
        }

        private void Start()
        {
            ApplyConfig();
        }

        private void Update()
        {
            if (_config != null && _panelRoot != null && Mathf.Abs(_config.Scale - _lastAppliedScale) > 0.001f)
            {
                ApplyScale();
            }

            if (!IsVisible || _hud == null) return;

            var current = _hud.CurrentSample;

            SetText(_fpsText, "FPS", current.Fps.ToString("F1"), current.Fps < _config.FpsWarningThreshold);
            SetText(_unityHeapText, "Unity Heap", FormatBytes(current.UnityHeapBytes), current.UnityHeapBytes > _config.UnityHeapWarningThresholdMB * 1024 * 1024);
            SetText(_drawCallText, "DrawCall", current.DrawCalls.ToString(), false);
            SetText(_triangleText, "Triangles", FormatCount(current.TriangleCount), false);

            string gcAllocValue = FormatBytes(current.GcAllocBytes);
            if (_config.CaptureAudioMemory && current.AudioMemoryBytes > 0)
            {
                gcAllocValue += $" · Audio {FormatBytes(current.AudioMemoryBytes)}";
            }
            bool gcWarning = current.GcAllocBytes > _config.GcAllocWarningThresholdKB * 1024
                || (_config.CaptureAudioMemory && current.AudioMemoryBytes > _config.AudioMemoryWarningThresholdMB * 1024 * 1024);
            SetText(_gcAllocText, "GC Alloc", gcAllocValue, gcWarning);

            if (current.JsHeapBytes > 0)
            {
                SetText(_jsHeapText, "JS Heap", FormatBytes(current.JsHeapBytes), current.JsHeapBytes > _config.JsHeapWarningThresholdMB * 1024 * 1024);
                _jsHeapText.gameObject.SetActive(true);
            }
            else if (_jsHeapText != null)
            {
                _jsHeapText.gameObject.SetActive(false);
            }

            UpdateLeakWarning();

            if (_startupText != null)
            {
                string startup = _hud.StartupSummary;
                _startupText.gameObject.SetActive(!string.IsNullOrEmpty(startup));
                _startupText.text = startup;
            }

            if (_graph != null && _config != null && _config.ShowHistoryGraph)
            {
                _graph.UpdateGraph(_hud.GetHistory(), _config.GraphMaxPoints);
            }
        }

        private void ApplyConfig()
        {
            if (_config == null) return;

            _canvas.sortingOrder = 9999;

            if (_scaler != null)
            {
                _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                _scaler.referenceResolution = new Vector2(1920f, 1080f);
                _scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                _scaler.matchWidthOrHeight = 0f;
                _scaler.scaleFactor = 1f;
            }

            if (_rectTransform != null)
            {
                _rectTransform.anchorMin = Vector2.zero;
                _rectTransform.anchorMax = Vector2.one;
                _rectTransform.offsetMin = Vector2.zero;
                _rectTransform.offsetMax = Vector2.zero;
                _rectTransform.localScale = Vector3.one;
            }

            if (_panelRoot != null)
            {
                _panelImage = _panelRoot.GetComponent<Image>();
                if (_panelImage != null)
                {
                    _panelBaseColor = _panelImage.color;
                }

                var anchor = _config.DefaultAnchor;
                _panelRoot.anchorMin = new Vector2(anchor.x, anchor.y);
                _panelRoot.anchorMax = new Vector2(anchor.x, anchor.y);
                float pivotX = anchor.x < 0.5f ? 0f : 1f;
                float pivotY = anchor.y < 0.5f ? 0f : 1f;
                _panelRoot.pivot = new Vector2(pivotX, pivotY);
                _panelRoot.anchoredPosition = Vector2.zero;
                _panelRoot.localScale = Vector3.one;

                ApplyScale();
                SetupDrag();

                if (_startupText != null)
                {
                    _startupText.gameObject.SetActive(_config.ShowStartupSummary);
                }

                if (_graph != null)
                {
                    _graph.gameObject.SetActive(_config.ShowHistoryGraph);
                }
            }

            IsVisible = _config.VisibleOnStart;
            Debug.Log($"[MPSPerformanceUI] ApplyConfig Scale={_config.Scale}");
        }

        private void ApplyScale()
        {
            if (_panelRoot == null || _config == null) return;

            float scale = _config.Scale;
            _lastAppliedScale = scale;
            _panelRoot.sizeDelta = new Vector2(240f * scale, 220f * scale);

            var texts = _panelRoot.GetComponentsInChildren<Text>(true);
            float[] baseY = { 0f, -24f, -48f, -72f, -96f, -120f, -144f };
            for (int i = 0; i < texts.Length && i < baseY.Length; i++)
            {
                var text = texts[i];
                var textRect = text.rectTransform;
                textRect.anchoredPosition = new Vector2(0f, baseY[i] * scale);
                textRect.sizeDelta = new Vector2(-8f * scale, 20f * scale);
                text.fontSize = Mathf.RoundToInt(14f * scale);
                text.horizontalOverflow = HorizontalWrapMode.Overflow;
                text.verticalOverflow = VerticalWrapMode.Overflow;
            }

            if (_graph != null)
            {
                var graphRect = _graph.GetComponent<RectTransform>();
                if (graphRect != null)
                {
                    graphRect.anchoredPosition = new Vector2(0f, -170f * scale);
                    graphRect.sizeDelta = new Vector2(-8f * scale, 40f * scale);
                }
            }
        }

        private void UpdateLeakWarning()
        {
            if (_panelImage == null || _config == null) return;

            var report = MiniGame.PerformanceSuite.Runtime.Memory.MPSMemoryLeakDetector.LastReport;
            bool isLeaking = _config.ShowLeakWarningOnHud && report != null && report.HasLeak;
            _panelImage.color = isLeaking ? new Color(0.6f, 0f, 0f, _panelBaseColor.a) : _panelBaseColor;
        }

        private void SetupDrag()
        {
            if (_panelRoot == null) return;

            var trigger = _panelRoot.GetComponent<EventTrigger>();
            if (!_config.Draggable)
            {
                if (trigger != null) Destroy(trigger);
                return;
            }

            if (trigger != null) return;

            trigger = _panelRoot.gameObject.AddComponent<EventTrigger>();

            var beginDragEntry = new EventTrigger.Entry { eventID = EventTriggerType.BeginDrag };
            beginDragEntry.callback.AddListener((data) => OnBeginDrag((PointerEventData)data));
            trigger.triggers.Add(beginDragEntry);

            var dragEntry = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
            dragEntry.callback.AddListener((data) => OnDrag((PointerEventData)data));
            trigger.triggers.Add(dragEntry);
        }

        private void OnBeginDrag(PointerEventData eventData)
        {
            if (_panelRoot == null) return;
            _dragOffset = _panelRoot.anchoredPosition - GetLocalPoint(eventData);
        }

        private void OnDrag(PointerEventData eventData)
        {
            if (_panelRoot == null) return;
            _panelRoot.anchoredPosition = GetLocalPoint(eventData) + _dragOffset;
        }

        private Vector2 GetLocalPoint(PointerEventData eventData)
        {
            var parent = _panelRoot.parent as RectTransform;
            if (parent == null) return Vector2.zero;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parent, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);
            return localPoint;
        }

        private void SetText(Text text, string label, string value, bool warning)
        {
            if (text == null) return;
            text.text = $"{label}: {value}";
            text.color = warning ? Color.red : Color.white;
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
