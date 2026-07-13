using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.StarterKit.Runtime.UI
{
    /// <summary>
    /// UI 管理器。提供窗口打开/关闭、层级管理、缓存复用。
    /// </summary>
    public class MGSUIManager : MonoBehaviour
    {
        private static MGSUIManager _instance;

        public static MGSUIManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("MGSUIManager");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<MGSUIManager>();
                    _instance.InitializeRoot();
                }
                return _instance;
            }
        }

        [SerializeField]
        private Transform _root;

        private readonly Dictionary<string, GameObject> _cachedPanels = new Dictionary<string, GameObject>();
        private readonly Dictionary<string, int> _layerOrders = new Dictionary<string, int>
        {
            { "Background", 0 },
            { "Default", 100 },
            { "Popup", 200 },
            { "Overlay", 300 }
        };

        private void InitializeRoot()
        {
            if (_root != null) return;

            var canvas = new GameObject("MGSUICanvas", typeof(Canvas), typeof(UnityEngine.UI.GraphicRaycaster));
            var c = canvas.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 1000;
            DontDestroyOnLoad(canvas);
            _root = canvas.transform;

            var scaler = canvas.gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
        }

        /// <summary>
        /// 打开一个 UI 面板。如果已缓存则复用，否则从 Prefab 实例化。
        /// </summary>
        public GameObject OpenPanel(string panelId, GameObject prefab = null, string layer = "Default")
        {
            if (_cachedPanels.TryGetValue(panelId, out var existing))
            {
                existing.SetActive(true);
                SetLayer(existing.transform, layer);
                return existing;
            }

            if (prefab == null)
            {
                Debug.LogWarning($"[MGSUIManager] Panel '{panelId}' not cached and no prefab provided.");
                return null;
            }

            var instance = Instantiate(prefab, _root);
            instance.name = panelId;
            _cachedPanels[panelId] = instance;
            SetLayer(instance.transform, layer);
            return instance;
        }

        /// <summary>
        /// 关闭一个 UI 面板。
        /// </summary>
        public void ClosePanel(string panelId)
        {
            if (_cachedPanels.TryGetValue(panelId, out var panel))
            {
                panel.SetActive(false);
            }
        }

        /// <summary>
        /// 销毁一个 UI 面板。
        /// </summary>
        public void DestroyPanel(string panelId)
        {
            if (_cachedPanels.TryGetValue(panelId, out var panel))
            {
                Destroy(panel);
                _cachedPanels.Remove(panelId);
            }
        }

        /// <summary>
        /// 关闭所有面板。
        /// </summary>
        public void CloseAllPanels()
        {
            foreach (var pair in _cachedPanels)
            {
                if (pair.Value != null)
                {
                    pair.Value.SetActive(false);
                }
            }
        }

        private void SetLayer(Transform panel, string layer)
        {
            if (!_layerOrders.TryGetValue(layer, out int order))
            {
                order = 100;
            }

            // 通过子 Canvas 控制层级
            var canvas = panel.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = panel.gameObject.AddComponent<Canvas>();
                panel.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }
            canvas.overrideSorting = true;
            canvas.sortingOrder = order;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
