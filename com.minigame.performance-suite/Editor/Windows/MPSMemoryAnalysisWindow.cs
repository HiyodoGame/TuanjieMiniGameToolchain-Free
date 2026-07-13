#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MiniGame.Core.Editor.Licensing;
using MiniGame.PerformanceSuite.Runtime.Data;
using MiniGame.PerformanceSuite.Runtime.Memory;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace MiniGame.PerformanceSuite.Editor.Windows
{
    /// <summary>
    /// 内存分析主面板：实时趋势、iOS 内存预警、资源排名、报告导出。
    /// </summary>
    public class MPSMemoryAnalysisWindow : EditorWindow
    {
        private const int MaxSamples = 60;
        private const int TopAssetCount = 10;

        [MenuItem("Window/MiniGame/Performance Suite/Memory Analysis")]
        private static void ShowWindow()
        {
            var window = GetWindow<MPSMemoryAnalysisWindow>("Memory Analysis");
            window.minSize = new Vector2(560, 480);
            window.Show();
        }

        private readonly List<MemorySample> _samples = new List<MemorySample>();
        private readonly List<MPSMemoryAnalysisAssetEntry> _resourceRankings = new List<MPSMemoryAnalysisAssetEntry>();

        private bool _autoRefresh = true;
        private float _lastSampleTime;
        private int _selectedDeviceIndex;
        private float _wasmSizeMB = 30f;
        private Vector2 _scrollPosition;

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            _selectedDeviceIndex = 0;
            RefreshResourceRankings();
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            if (!_autoRefresh) return;
            if (EditorApplication.timeSinceStartup - _lastSampleTime < 1.0) return;

            _lastSampleTime = (float)EditorApplication.timeSinceStartup;
            AddSample();
            Repaint();
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawToolbar();
            EditorGUILayout.Space();

            DrawMetrics();
            EditorGUILayout.Space();

            DrawGraph();
            EditorGUILayout.Space();

            DrawIOSWarning();
            EditorGUILayout.Space();

            DrawResourceRankings();
            EditorGUILayout.Space();

            DrawSnapshotActions();

            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            _autoRefresh = GUILayout.Toggle(_autoRefresh, "Auto Refresh", EditorStyles.toolbarButton);

            if (GUILayout.Button("Manual Sample", EditorStyles.toolbarButton))
            {
                AddSample();
                Repaint();
            }

            if (GUILayout.Button("Refresh Rankings", EditorStyles.toolbarButton))
            {
                RefreshResourceRankings();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawMetrics()
        {
            EditorGUILayout.LabelField("Real-time Memory", EditorStyles.boldLabel);

            var current = _samples.Count > 0 ? _samples[_samples.Count - 1] : CaptureSample();

            EditorGUILayout.BeginHorizontal("box");
            DrawMetricBox("Unity Heap", current.UnityHeapBytes, Color.cyan);
            DrawMetricBox("Mono Heap", current.MonoHeapBytes, Color.yellow);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal("box");
            DrawMetricBox("JS Heap", current.JsHeapBytes, Color.green);
            DrawMetricBox("GFX Driver", current.GfxDriverBytes, Color.magenta);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawMetricBox(string label, long bytes, Color color)
        {
            EditorGUILayout.BeginVertical("box", GUILayout.MinWidth(180f));
            var prev = GUI.color;
            GUI.color = color;
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            GUI.color = prev;
            EditorGUILayout.LabelField(MPSMemoryAnalysisReport.FormatBytes(bytes));
            EditorGUILayout.EndVertical();
        }

        private void DrawGraph()
        {
            EditorGUILayout.LabelField("Unity Heap Trend (60s)", EditorStyles.boldLabel);

            var rect = EditorGUILayout.GetControlRect(false, 120f);
            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f, 1f));

            if (_samples.Count < 2) return;

            long maxValue = 1;
            foreach (var sample in _samples)
            {
                if (sample.UnityHeapBytes > maxValue) maxValue = sample.UnityHeapBytes;
            }

            var prevColor = GUI.color;
            GUI.color = Color.cyan;

            for (int i = 0; i < _samples.Count - 1; i++)
            {
                float x0 = rect.x + rect.width * i / (MaxSamples - 1);
                float x1 = rect.x + rect.width * (i + 1) / (MaxSamples - 1);
                float y0 = rect.y + rect.height * (1f - _samples[i].UnityHeapBytes / (float)maxValue);
                float y1 = rect.y + rect.height * (1f - _samples[i + 1].UnityHeapBytes / (float)maxValue);

                DrawLine(new Vector2(x0, y0), new Vector2(x1, y1), 2f);
            }

            GUI.color = prevColor;

            EditorGUILayout.LabelField($"Max: {MPSMemoryAnalysisReport.FormatBytes(maxValue)}", EditorStyles.miniLabel);
        }

        private void DrawLine(Vector2 start, Vector2 end, float thickness)
        {
            var prev = Handles.color;
            Handles.color = GUI.color;
            Handles.DrawAAPolyLine(thickness, start, end);
            Handles.color = prev;
        }

        private void DrawIOSWarning()
        {
            EditorGUILayout.LabelField("iOS Memory Warning", EditorStyles.boldLabel);

            var deviceNames = MPSIOSMemoryLimits.GetAllDeviceNames();
            _selectedDeviceIndex = EditorGUILayout.Popup("Target Device", _selectedDeviceIndex, deviceNames);
            _wasmSizeMB = EditorGUILayout.Slider("WASM Size (MB)", _wasmSizeMB, 10f, 100f);

            var deviceId = deviceNames[_selectedDeviceIndex];
            var limitBytes = MPSIOSMemoryLimits.GetLimit(deviceId) ?? MPSIOSMemoryLimits.DefaultLimitBytes;

            var current = _samples.Count > 0 ? _samples[_samples.Count - 1] : CaptureSample();
            var wasmCompileBytes = (long)(_wasmSizeMB * 1024 * 1024 * 8);
            var estimatedTotal = current.UnityHeapBytes + current.MonoHeapBytes + current.GfxDriverBytes + wasmCompileBytes;
            var ratio = estimatedTotal / (float)limitBytes;

            EditorGUILayout.LabelField($"Device Limit: {MPSMemoryAnalysisReport.FormatBytes(limitBytes)}");
            EditorGUILayout.LabelField($"Estimated Total: {MPSMemoryAnalysisReport.FormatBytes(estimatedTotal)}");

            Color barColor;
            string status;
            MessageType messageType;
            if (ratio >= 0.95f)
            {
                barColor = Color.red;
                status = "CRITICAL: 接近/超过 iOS 内存上限";
                messageType = MessageType.Error;
            }
            else if (ratio >= 0.8f)
            {
                barColor = Color.yellow;
                status = "WARNING: 接近 iOS 内存上限";
                messageType = MessageType.Warning;
            }
            else
            {
                barColor = Color.green;
                status = "OK: 内存安全";
                messageType = MessageType.Info;
            }

            DrawProgressBar(ratio, $"{ratio * 100f:F1}%", barColor);
            EditorGUILayout.HelpBox(status, messageType);
        }

        private void DrawProgressBar(float ratio, string label, Color color)
        {
            var rect = EditorGUILayout.GetControlRect(false, 18f);
            EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f, 1f));
            var fillRect = new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(ratio), rect.height);
            EditorGUI.DrawRect(fillRect, color);

            var labelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            EditorGUI.LabelField(rect, label, labelStyle);
        }

        private void DrawResourceRankings()
        {
            EditorGUILayout.LabelField("Resource Memory Rankings", EditorStyles.boldLabel);

            if (_resourceRankings.Count == 0)
            {
                EditorGUILayout.HelpBox("点击 Refresh Rankings 统计资源内存占用。", MessageType.Info);
                return;
            }

            var grouped = _resourceRankings
                .GroupBy(e => e.Type)
                .OrderByDescending(g => g.Sum(e => e.SizeBytes));

            foreach (var group in grouped)
            {
                var totalBytes = group.Sum(e => e.SizeBytes);
                EditorGUILayout.LabelField($"{group.Key}: {MPSMemoryAnalysisReport.FormatBytes(totalBytes)}", EditorStyles.boldLabel);

                foreach (var entry in group.Take(TopAssetCount))
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(entry.Name, GUILayout.Width(260f));
                    EditorGUILayout.LabelField(MPSMemoryAnalysisReport.FormatBytes(entry.SizeBytes));
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.Space();
            }
        }

        private void DrawSnapshotActions()
        {
            EditorGUILayout.LabelField("Snapshots", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Capture Snapshot"))
            {
                EnsureSnapshotManager();
                MPSMemorySnapshot.Capture($"Analysis_{DateTime.Now:HHmmss}");
            }

            if (GUILayout.Button("Open Snapshot Window"))
            {
                MPSMemorySnapshotWindow.ShowWindow();
            }

            if (GUILayout.Button("Export Report"))
            {
                if (!MiniGameLicenseManager.IsFeatureEnabled(MiniGameLicenseFeature.PerformanceSuiteMemoryHistory))
                {
                    var hint = MiniGameLicenseManager.GetUpgradeHint(MiniGameLicenseFeature.PerformanceSuiteMemoryHistory);
                    Debug.LogWarning($"[PerformanceSuite] {hint}");
                    EditorUtility.DisplayDialog("功能未授权", hint, "确定");
                }
                else
                {
                    ExportReport();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void ExportReport()
        {
            var deviceNames = MPSIOSMemoryLimits.GetAllDeviceNames();
            var deviceId = deviceNames[Mathf.Clamp(_selectedDeviceIndex, 0, deviceNames.Length - 1)];
            var limitBytes = MPSIOSMemoryLimits.GetLimit(deviceId) ?? MPSIOSMemoryLimits.DefaultLimitBytes;

            var current = _samples.Count > 0 ? _samples[_samples.Count - 1] : CaptureSample();
            var wasmCompileBytes = (long)(_wasmSizeMB * 1024 * 1024 * 8);

            var report = MPSMemoryAnalysisReport.Capture(
                deviceId,
                limitBytes,
                current.UnityHeapBytes,
                current.MonoHeapBytes,
                current.JsHeapBytes,
                current.GfxDriverBytes,
                wasmCompileBytes,
                _resourceRankings);

            var path = EditorUtility.SaveFilePanel("Export Memory Analysis Report", "", "memory_analysis", "md");
            if (!string.IsNullOrEmpty(path))
            {
                report.SaveMarkdown(path);
                Debug.Log($"[MPSMemoryAnalysisWindow] Report exported to {path}");
            }
        }

        private void AddSample()
        {
            _samples.Add(CaptureSample());
            while (_samples.Count > MaxSamples)
            {
                _samples.RemoveAt(0);
            }
        }

        private MemorySample CaptureSample()
        {
            long jsHeap = 0;
            try
            {
                jsHeap = Runtime.Bridge.MPSWeChatBridge.GetUsedJSHeapSize();
                if (jsHeap < 0) jsHeap = 0;
            }
            catch
            {
                jsHeap = 0;
            }

            long gfxDriver = 0;
            try
            {
                gfxDriver = Profiler.GetAllocatedMemoryForGraphicsDriver();
            }
            catch
            {
                gfxDriver = 0;
            }

            return new MemorySample
            {
                Time = (float)EditorApplication.timeSinceStartup,
                UnityHeapBytes = Profiler.GetTotalAllocatedMemoryLong(),
                MonoHeapBytes = Profiler.GetMonoUsedSizeLong(),
                JsHeapBytes = jsHeap,
                GfxDriverBytes = gfxDriver
            };
        }

        private void RefreshResourceRankings()
        {
            _resourceRankings.Clear();

            CollectAssetsOfType<Texture2D>("Texture");
            CollectAssetsOfType<AudioClip>("Audio");
            CollectAssetsOfType<Mesh>("Mesh");
            CollectAssetsOfType<Material>("Material");
            CollectAssetsOfType<Shader>("Shader");
            CollectAssetsOfType<Sprite>("Sprite");

            _resourceRankings.Sort((a, b) => b.SizeBytes.CompareTo(a.SizeBytes));
        }

        private void CollectAssetsOfType<T>(string typeName) where T : UnityEngine.Object
        {
            var objects = Resources.FindObjectsOfTypeAll<T>();
            foreach (var obj in objects)
            {
                if (obj == null) continue;
                long size = 0;
                try
                {
                    size = Profiler.GetRuntimeMemorySizeLong(obj);
                }
                catch
                {
                    size = 0;
                }

                if (size <= 0) continue;

                _resourceRankings.Add(new MPSMemoryAnalysisAssetEntry
                {
                    Type = typeName,
                    Name = obj.name,
                    SizeBytes = size
                });
            }
        }

        private void EnsureSnapshotManager()
        {
            if (MPSMemorySnapshot.Snapshots != null) return;

            var config = ScriptableObject.CreateInstance<MPSPerformanceConfig>();
            config.AutoMemorySnapshot = false;
            MPSMemorySnapshot.Initialize(config);
        }

        private class MemorySample
        {
            public float Time;
            public long UnityHeapBytes;
            public long MonoHeapBytes;
            public long JsHeapBytes;
            public long GfxDriverBytes;
        }
    }
}
#endif
