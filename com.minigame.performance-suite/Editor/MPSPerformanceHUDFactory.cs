#if UNITY_EDITOR
using System;
using MiniGame.PerformanceSuite.Runtime.Data;
using MiniGame.PerformanceSuite.Runtime.HUD;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MiniGame.PerformanceSuite.Editor
{
    /// <summary>
    /// 生成 Performance HUD 默认预置体与配置资源。
    /// </summary>
    public class MPSPerformanceHUDFactory
    {
        private const string PackageResources = "Packages/com.minigame.performance-suite/Resources";
        private const string ConfigFolder = PackageResources + "/MiniGame.PerformanceSuite";
        private const string ConfigPath = ConfigFolder + "/MPSPerformanceConfig.asset";
        private const string PrefabPath = ConfigFolder + "/MPSPerformanceHUD.prefab";

        [MenuItem("Assets/Create/MiniGame/Performance Suite/Create HUD Assets")]
        [MenuItem("Window/MiniGame/Performance Suite/Create HUD Assets")]
        public static void CreateAssets()
        {
            try
            {
                EnsureFolderExists();
                CreateConfig();
                CreatePrefab();
                AssetDatabase.Refresh();
                Debug.Log("[MPSPerformanceHUDFactory] HUD assets created.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MPSPerformanceHUDFactory] Failed to create HUD assets: {ex}");
            }
        }

        private static void CreateConfig()
        {
            var existing = AssetDatabase.LoadAssetAtPath<MPSPerformanceConfig>(ConfigPath);
            if (existing != null)
            {
                Debug.Log("[MPSPerformanceHUDFactory] Config already exists, skipped.");
                return;
            }

            var config = ScriptableObject.CreateInstance<MPSPerformanceConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            EditorUtility.SetDirty(config);
            Debug.Log($"[MPSPerformanceHUDFactory] Config created at {ConfigPath}");
        }

        private static void CreatePrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (existing != null)
            {
                AssetDatabase.DeleteAsset(PrefabPath);
                AssetDatabase.Refresh();
                Debug.Log("[MPSPerformanceHUDFactory] Existing prefab removed, regenerating.");
            }

            var config = AssetDatabase.LoadAssetAtPath<MPSPerformanceConfig>(ConfigPath);

            var root = new GameObject("MPSPerformanceHUD", typeof(MPSPerformanceHUD));
            root.SetActive(true);
            var hud = root.GetComponent<MPSPerformanceHUD>();

            var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(GraphicRaycaster), typeof(CanvasScaler), typeof(MPSPerformanceUI));
            canvasGO.SetActive(true);
            canvasGO.transform.SetParent(root.transform, false);

            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;

            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            var panelGO = new GameObject("Panel", typeof(Image));
            panelGO.SetActive(true);
            panelGO.transform.SetParent(canvasGO.transform, false);
            var panelRect = panelGO.GetComponent<RectTransform>();
            var anchor = config != null ? config.DefaultAnchor : new Vector2(0.02f, 0.98f);
            panelRect.anchorMin = new Vector2(anchor.x, anchor.y);
            panelRect.anchorMax = new Vector2(anchor.x, anchor.y);
            float pivotX = anchor.x < 0.5f ? 0f : 1f;
            float pivotY = anchor.y < 0.5f ? 0f : 1f;
            panelRect.pivot = new Vector2(pivotX, pivotY);
            panelRect.sizeDelta = new Vector2(240, 220);
            panelRect.anchoredPosition = Vector2.zero;

            var image = panelGO.GetComponent<Image>();
            image.color = new Color(0, 0, 0, 0.75f);

            var ui = canvasGO.GetComponent<MPSPerformanceUI>();
            ui.Setup(config, hud);

            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            var fpsText = CreateText(panelGO.transform, "FpsText", "FPS: --", new Vector2(0, 0), defaultFont);
            var heapText = CreateText(panelGO.transform, "UnityHeapText", "Unity Heap: --", new Vector2(0, -24), defaultFont);
            var jsHeapText = CreateText(panelGO.transform, "JsHeapText", "JS Heap: --", new Vector2(0, -48), defaultFont);
            var drawCallText = CreateText(panelGO.transform, "DrawCallText", "DrawCall: --", new Vector2(0, -72), defaultFont);
            var triangleText = CreateText(panelGO.transform, "TriangleText", "Triangles: --", new Vector2(0, -96), defaultFont);
            var gcAllocText = CreateText(panelGO.transform, "GcAllocText", "GC Alloc: --", new Vector2(0, -120), defaultFont);
            var startupText = CreateText(panelGO.transform, "StartupText", "", new Vector2(0, -144), defaultFont);
            startupText.gameObject.SetActive(false);

            var graphGO = new GameObject("Graph", typeof(RawImage), typeof(MPSPerformanceGraph));
            graphGO.transform.SetParent(panelGO.transform, false);
            var graphRect = graphGO.GetComponent<RectTransform>();
            graphRect.anchorMin = new Vector2(0, 1);
            graphRect.anchorMax = new Vector2(1, 1);
            graphRect.pivot = new Vector2(0, 1);
            graphRect.sizeDelta = new Vector2(-8, 40);
            graphRect.anchoredPosition = new Vector2(0, -170);
            var graph = graphGO.GetComponent<MPSPerformanceGraph>();

            var serializedUI = new SerializedObject(ui);
            serializedUI.FindProperty("_fpsText").objectReferenceValue = fpsText;
            serializedUI.FindProperty("_unityHeapText").objectReferenceValue = heapText;
            serializedUI.FindProperty("_jsHeapText").objectReferenceValue = jsHeapText;
            serializedUI.FindProperty("_drawCallText").objectReferenceValue = drawCallText;
            serializedUI.FindProperty("_triangleText").objectReferenceValue = triangleText;
            serializedUI.FindProperty("_gcAllocText").objectReferenceValue = gcAllocText;
            serializedUI.FindProperty("_startupText").objectReferenceValue = startupText;
            serializedUI.FindProperty("_graph").objectReferenceValue = graph;
            serializedUI.FindProperty("_panelRoot").objectReferenceValue = panelRect;
            serializedUI.ApplyModifiedProperties();

            var savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);

            if (savedPrefab == null)
            {
                throw new InvalidOperationException($"Failed to save prefab at {PrefabPath}");
            }

            Debug.Log($"[MPSPerformanceHUDFactory] Prefab created at {PrefabPath}");
        }

        private static void EnsureFolderExists()
        {
            string physicalFolder = ConfigFolder.Replace('/', System.IO.Path.DirectorySeparatorChar);
            if (!System.IO.Directory.Exists(physicalFolder))
            {
                System.IO.Directory.CreateDirectory(physicalFolder);
                AssetDatabase.Refresh();
            }
        }

        private static Text CreateText(Transform parent, string name, string initialText, Vector2 anchoredPosition, Font font)
        {
            var go = new GameObject(name, typeof(Text));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0, 1);
            rect.sizeDelta = new Vector2(0, 20);
            rect.anchoredPosition = anchoredPosition;

            var text = go.GetComponent<Text>();
            text.text = initialText;
            text.font = font;
            text.fontSize = 14;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;

            return text;
        }
    }
}
#endif
