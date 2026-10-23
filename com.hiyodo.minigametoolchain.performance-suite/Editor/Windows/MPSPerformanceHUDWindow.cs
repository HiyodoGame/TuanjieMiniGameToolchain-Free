#if UNITY_EDITOR
using MiniGame.Core.Editor;
using MiniGame.Core.Editor.Licensing;
using MiniGame.PerformanceSuite.Runtime.Data;
using MiniGame.PerformanceSuite.Runtime.HUD;
using UnityEditor;
using UnityEngine;

namespace MiniGame.PerformanceSuite.Editor.Windows
{
    /// <summary>
    /// 编辑器内预览与测试 Performance HUD 的窗口。
    /// </summary>
    public class MPSPerformanceHUDWindow : EditorWindow
    {
        private MPSPerformanceConfig _config;
        private bool _simulateData;
        private float _simulatedFps = 55f;
        private float _simulatedHeapMB = 256f;
        private float _simulatedJsHeapMB = 128f;
        private int _simulatedDrawCalls = 120;
        private int _simulatedTriangles = 45000;

        [MenuItem("Window/MiniGame/Performance Suite/HUD Preview")]
        private static void Open()
        {
            var window = GetWindow<MPSPerformanceHUDWindow>("MPS HUD Preview");
            window.minSize = new Vector2(300, 350);
            MiniGameBranding.SetTitleIcon(window, "MPS HUD Preview");
            window.Show();
        }

        private void OnGUI()
        {
            MiniGameBranding.DrawHeader("Performance HUD Preview", "编辑器内预览运行时性能 HUD");
            EditorGUILayout.Space();

            _config = EditorGUILayout.ObjectField("Config", _config, typeof(MPSPerformanceConfig), false) as MPSPerformanceConfig;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Runtime Control", EditorStyles.boldLabel);

            if (GUILayout.Button("Create / Show HUD"))
            {
                CreateOrShowHud();
            }

            if (GUILayout.Button("Hide HUD") && MPSPerformanceHUD.Instance != null)
            {
                MPSPerformanceHUD.Instance.SetVisible(false);
            }

            if (GUILayout.Button("Destroy HUD") && MPSPerformanceHUD.Instance != null)
            {
                DestroyImmediate(MPSPerformanceHUD.Instance.gameObject);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Export", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Export JSON") && MPSPerformanceHUD.Instance != null)
            {
                if (!MiniGameLicenseManager.IsFeatureEnabled(MiniGameLicenseFeature.PerformanceSuiteMemoryHistory))
                {
                    var hint = MiniGameLicenseManager.GetUpgradeHint(MiniGameLicenseFeature.PerformanceSuiteMemoryHistory);
                    Debug.LogWarning($"[PerformanceSuite] {hint}");
                    EditorUtility.DisplayDialog("功能未授权", hint, "确定");
                }
                else
                {
                    string path = EditorUtility.SaveFilePanel("Export HUD History", "", "hud_history", "json");
                    if (!string.IsNullOrEmpty(path))
                    {
                        System.IO.File.WriteAllText(path, MPSPerformanceHUD.Instance.ExportHistoryToJson());
                        Debug.Log($"[MPSPerformanceHUDWindow] Exported JSON to {path}");
                    }
                }
            }

            if (GUILayout.Button("Export CSV") && MPSPerformanceHUD.Instance != null)
            {
                if (!MiniGameLicenseManager.IsFeatureEnabled(MiniGameLicenseFeature.PerformanceSuiteMemoryHistory))
                {
                    var hint = MiniGameLicenseManager.GetUpgradeHint(MiniGameLicenseFeature.PerformanceSuiteMemoryHistory);
                    Debug.LogWarning($"[PerformanceSuite] {hint}");
                    EditorUtility.DisplayDialog("功能未授权", hint, "确定");
                }
                else
                {
                    string path = EditorUtility.SaveFilePanel("Export HUD History", "", "hud_history", "csv");
                    if (!string.IsNullOrEmpty(path))
                    {
                        System.IO.File.WriteAllText(path, MPSPerformanceHUD.Instance.ExportHistoryToCsv());
                        Debug.Log($"[MPSPerformanceHUDWindow] Exported CSV to {path}");
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Simulated Data", EditorStyles.boldLabel);
            _simulateData = EditorGUILayout.Toggle("Simulate", _simulateData);
            _simulatedFps = EditorGUILayout.Slider("FPS", _simulatedFps, 1f, 120f);
            _simulatedHeapMB = EditorGUILayout.Slider("Unity Heap (MB)", _simulatedHeapMB, 0f, 1024f);
            _simulatedJsHeapMB = EditorGUILayout.Slider("JS Heap (MB)", _simulatedJsHeapMB, 0f, 512f);
            _simulatedDrawCalls = EditorGUILayout.IntSlider("DrawCalls", _simulatedDrawCalls, 0, 500);
            _simulatedTriangles = EditorGUILayout.IntSlider("Triangles", _simulatedTriangles, 0, 200000);

            if (_simulateData && MPSPerformanceHUD.Instance != null)
            {
                // 通过反射写入模拟数据（仅编辑器预览）
                var hudType = typeof(MPSPerformanceHUD);
                var currentField = hudType.GetField("_currentSample", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                currentField?.SetValue(MPSPerformanceHUD.Instance, new MPSFrameSample
                {
                    Fps = _simulatedFps,
                    UnityHeapBytes = (long)(_simulatedHeapMB * 1024 * 1024),
                    JsHeapBytes = (long)(_simulatedJsHeapMB * 1024 * 1024),
                    DrawCalls = _simulatedDrawCalls,
                    TriangleCount = _simulatedTriangles
                });
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Current Sample", EditorStyles.boldLabel);
            if (MPSPerformanceHUD.Instance != null)
            {
                var sample = MPSPerformanceHUD.Instance.CurrentSample;
                EditorGUILayout.LabelField($"FPS: {sample.Fps:F1}");
                EditorGUILayout.LabelField($"Unity Heap: {sample.UnityHeapBytes / (1024f * 1024f):F1} MB");
                EditorGUILayout.LabelField($"JS Heap: {sample.JsHeapBytes / (1024f * 1024f):F1} MB");
                EditorGUILayout.LabelField($"DrawCalls: {sample.DrawCalls}");
                EditorGUILayout.LabelField($"Triangles: {sample.TriangleCount}");
            }
            else
            {
                EditorGUILayout.HelpBox("HUD not active. Click 'Create / Show HUD'.", MessageType.Info);
            }
        }

        private void CreateOrShowHud()
        {
            if (MPSPerformanceHUD.Instance != null)
            {
                MPSPerformanceHUD.Instance.SetVisible(true);
                return;
            }

            if (_config == null)
            {
                _config = ScriptableObject.CreateInstance<MPSPerformanceConfig>();
                _config.VisibleOnStart = true;
            }

            var go = new GameObject("MPSPerformanceHUD");
            var hud = go.AddComponent<MPSPerformanceHUD>();
            var field = typeof(MPSPerformanceHUD).GetField("_config", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(hud, _config);
        }
    }
}
#endif
