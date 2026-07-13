using System.Collections.Generic;
using System.Linq;
using MiniGame.PerformanceSuite.Runtime.Data;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PerformanceSuite.Runtime.HUD
{
    /// <summary>
    /// 性能历史曲线图渲染器。将 FPS / Heap 历史数据渲染到 Texture2D。
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public class MPSPerformanceGraph : MonoBehaviour
    {
        [SerializeField]
        private int _width = 128;

        [SerializeField]
        private int _height = 64;

        [SerializeField]
        private Color _backgroundColor = new Color(0, 0, 0, 0.5f);

        [SerializeField]
        private Color _fpsColor = Color.green;

        [SerializeField]
        private Color _heapColor = Color.yellow;

        private RawImage _rawImage;
        private Texture2D _texture;
        private Color[] _pixels;

        private void Awake()
        {
            _rawImage = GetComponent<RawImage>();
            _texture = new Texture2D(_width, _height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point
            };
            _pixels = new Color[_width * _height];
            _rawImage.texture = _texture;
        }

        /// <summary>
        /// 更新曲线图。
        /// </summary>
        /// <param name="history">历史采样数据。</param>
        /// <param name="maxPoints">最多绘制的点数。</param>
        public void UpdateGraph(IReadOnlyList<MPSFrameSample> history, int maxPoints)
        {
            if (_texture == null || history == null || history.Count == 0) return;

            ClearBackground();

            var points = history.TakeLast(maxPoints).ToArray();
            if (points.Length < 2) return;

            float maxFps = Mathf.Max(points.Max(p => p.Fps), 60f);
            long maxHeap = points.Max(p => p.UnityHeapBytes);
            if (maxHeap <= 0) maxHeap = 1;

            for (int i = 0; i < points.Length - 1; i++)
            {
                int x0 = MapX(i, points.Length);
                int x1 = MapX(i + 1, points.Length);

                float fps0 = Mathf.Clamp01(points[i].Fps / maxFps);
                float fps1 = Mathf.Clamp01(points[i + 1].Fps / maxFps);
                DrawLine(x0, MapY(fps0), x1, MapY(fps1), _fpsColor);

                float heap0 = Mathf.Clamp01((float)points[i].UnityHeapBytes / maxHeap);
                float heap1 = Mathf.Clamp01((float)points[i + 1].UnityHeapBytes / maxHeap);
                DrawLine(x0, MapY(heap0), x1, MapY(heap1), _heapColor);
            }

            _texture.SetPixels(_pixels);
            _texture.Apply();
        }

        private void ClearBackground()
        {
            for (int i = 0; i < _pixels.Length; i++)
            {
                _pixels[i] = _backgroundColor;
            }
        }

        private int MapX(int index, int count)
        {
            return Mathf.RoundToInt((float)index / (count - 1) * (_width - 1));
        }

        private int MapY(float normalizedValue)
        {
            return Mathf.RoundToInt(normalizedValue * (_height - 1));
        }

        private void DrawLine(int x0, int y0, int x1, int y1, Color color)
        {
            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                SetPixel(x0, y0, color);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }
        }

        private void SetPixel(int x, int y, Color color)
        {
            if (x < 0 || x >= _width || y < 0 || y >= _height) return;
            _pixels[x + y * _width] = color;
        }

        private void OnDestroy()
        {
            if (_texture != null)
            {
                Destroy(_texture);
            }
        }
    }
}
