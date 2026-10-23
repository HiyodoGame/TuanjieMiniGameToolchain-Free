#if UNITY_EDITOR
using System;
using System.Linq;
using MiniGame.Core.Editor;
using MiniGame.Core.Editor.Licensing;
using MiniGame.PerformanceSuite.Runtime.Memory;
using UnityEditor;
using UnityEngine;

namespace MiniGame.PerformanceSuite.Editor.Windows
{
    /// <summary>
    /// 内存快照分析器编辑器窗口。
    /// </summary>
    public class MPSMemorySnapshotWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private int _baseIndex = -1;
        private int _compareIndex = -1;

        [MenuItem("Window/MiniGame/Performance Suite/Memory Snapshot")]
        public static void ShowWindow()
        {
            var window = GetWindow<MPSMemorySnapshotWindow>("Memory Snapshot");
            MiniGameBranding.SetTitleIcon(window, "Memory Snapshot");
            window.Show();
        }

        private void OnGUI()
        {
            MiniGameBranding.DrawHeader("内存快照", "抓取与对比微信小游戏内存快照");
            DrawToolbar();

            var snapshots = MPSMemorySnapshot.Snapshots;
            if (snapshots == null || snapshots.Count == 0)
            {
                EditorGUILayout.HelpBox("暂无内存快照。点击 Capture 手动抓取，或在 Play 模式下等待 Heap 超阈值自动触发。", MessageType.Info);
                return;
            }

            DrawSnapshotList(snapshots);
            DrawDiffSection(snapshots);
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Capture", EditorStyles.toolbarButton))
            {
                MPSMemorySnapshot.Capture($"Manual_{DateTime.Now:HHmmss}");
                Repaint();
            }

            if (GUILayout.Button("Clear", EditorStyles.toolbarButton))
            {
                MPSMemorySnapshot.Clear();
                _baseIndex = -1;
                _compareIndex = -1;
                Repaint();
            }

            if (GUILayout.Button("Export", EditorStyles.toolbarButton))
            {
                if (!MiniGameLicenseManager.IsFeatureEnabled(MiniGameLicenseFeature.PerformanceSuiteSnapshotDiff))
                {
                    var hint = MiniGameLicenseManager.GetUpgradeHint(MiniGameLicenseFeature.PerformanceSuiteSnapshotDiff);
                    Debug.LogWarning($"[PerformanceSuite] {hint}");
                    EditorUtility.DisplayDialog("功能未授权", hint, "确定");
                }
                else
                {
                    ExportSnapshots();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSnapshotList(System.Collections.Generic.IReadOnlyList<MPSMemorySnapshotData> snapshots)
        {
            EditorGUILayout.LabelField($"Snapshots: {snapshots.Count}", EditorStyles.boldLabel);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(200f));

            for (int i = snapshots.Count - 1; i >= 0; i--)
            {
                var snapshot = snapshots[i];
                EditorGUILayout.BeginHorizontal("box");

                EditorGUILayout.LabelField(snapshot.Label, GUILayout.Width(140f));
                EditorGUILayout.LabelField($"Unity: {FormatBytes(snapshot.UnityHeapBytes)}", GUILayout.Width(120f));
                EditorGUILayout.LabelField($"Mono: {FormatBytes(snapshot.MonoHeapBytes)}", GUILayout.Width(120f));

                if (snapshot.HasJsHeap)
                {
                    EditorGUILayout.LabelField($"JS: {FormatBytes(snapshot.JsHeapBytes)}", GUILayout.Width(120f));
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Base", EditorStyles.miniButton, GUILayout.Width(50f)))
                {
                    _baseIndex = i;
                }

                if (GUILayout.Button("Compare", EditorStyles.miniButton, GUILayout.Width(60f)))
                {
                    _compareIndex = i;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawDiffSection(System.Collections.Generic.IReadOnlyList<MPSMemorySnapshotData> snapshots)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Diff", EditorStyles.boldLabel);

            if (_baseIndex < 0 || _compareIndex < 0 || _baseIndex >= snapshots.Count || _compareIndex >= snapshots.Count)
            {
                EditorGUILayout.HelpBox("请选择 Base 和 Compare 快照。", MessageType.Info);
                return;
            }

            var diff = MPSMemorySnapshot.Diff(snapshots[_baseIndex], snapshots[_compareIndex]);
            DrawDeltaRow("Unity Heap", diff.UnityHeapDelta);
            DrawDeltaRow("Mono Heap", diff.MonoHeapDelta);
            DrawDeltaRow("JS Heap", diff.JsHeapDelta);
            DrawDeltaRow("Unused Reserved", diff.UnusedReservedMemoryDelta);
        }

        private void DrawDeltaRow(string label, long delta)
        {
            Color prevColor = GUI.color;
            if (delta > 0) GUI.color = Color.red;
            else if (delta < 0) GUI.color = Color.green;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(140f));
            EditorGUILayout.LabelField($"{FormatBytes(delta)} ({delta:+0;-0;0} B)", GUILayout.Width(180f));
            EditorGUILayout.EndHorizontal();

            GUI.color = prevColor;
        }

        private void ExportSnapshots()
        {
            var snapshots = MPSMemorySnapshot.Snapshots;
            if (snapshots == null || snapshots.Count == 0) return;

            string path = EditorUtility.SaveFilePanel("Export Memory Snapshots", "", "memory_snapshots", "json");
            if (string.IsNullOrEmpty(path)) return;

            var wrapper = new SnapshotListWrapper { Snapshots = snapshots.ToList() };
            string json = JsonUtility.ToJson(wrapper, true);
            System.IO.File.WriteAllText(path, json);
            Debug.Log($"[MPSMemorySnapshotWindow] Snapshots exported to {path}");
        }

        private static string FormatBytes(long bytes)
        {
            if (Mathf.Abs(bytes) < 1024) return $"{bytes} B";
            if (Mathf.Abs(bytes) < 1024 * 1024) return $"{bytes / 1024f:F1} KB";
            return $"{bytes / (1024f * 1024f):F1} MB";
        }

        [Serializable]
        private class SnapshotListWrapper
        {
            public System.Collections.Generic.List<MPSMemorySnapshotData> Snapshots;
        }
    }
}
#endif
