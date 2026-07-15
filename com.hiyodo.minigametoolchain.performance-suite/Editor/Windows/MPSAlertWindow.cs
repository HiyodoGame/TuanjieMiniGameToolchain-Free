#if UNITY_EDITOR
using MiniGame.Core.Editor;
using MiniGame.PerformanceSuite.Runtime.Core;
using UnityEditor;
using UnityEngine;

namespace MiniGame.PerformanceSuite.Editor.Windows
{
    /// <summary>
    /// 告警历史编辑器窗口。
    /// </summary>
    public class MPSAlertWindow : EditorWindow
    {
        private Vector2 _scrollPosition;

        [MenuItem("Window/MiniGame/Performance Suite/Alert History")]
        private static void ShowWindow()
        {
            var window = GetWindow<MPSAlertWindow>("Alert History");
            MiniGameBranding.SetTitleIcon(window, "Alert History");
            window.Show();
        }

        private void OnGUI()
        {
            MiniGameBranding.DrawHeader("Alert History", "查看运行时性能告警历史");

            if (GUILayout.Button("Clear History"))
            {
                // 通过反射清空历史（因为 _history 是 private）
                var historyField = typeof(MPSAlertDispatcher).GetField("_history", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                historyField?.GetValue(null);
                // 由于 List<T> 是引用类型，可直接调用 Clear
                var list = historyField?.GetValue(null) as System.Collections.IList;
                list?.Clear();
                Repaint();
            }

            var history = MPSAlertDispatcher.History;
            if (history == null || history.Count == 0)
            {
                EditorGUILayout.HelpBox("暂无告警记录。", MessageType.Info);
                return;
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            for (int i = history.Count - 1; i >= 0; i--)
            {
                var alert = history[i];
                DrawAlert(alert);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawAlert(MPSAlertMessage alert)
        {
            MessageType type = MessageType.Info;
            switch (alert.Level)
            {
                case MPSAlertLevel.Warning: type = MessageType.Warning; break;
                case MPSAlertLevel.Error: type = MessageType.Error; break;
            }

            EditorGUILayout.HelpBox($"[{alert.Timestamp}] [{alert.Category}] {alert.Title}\n{alert.Body}", type);
        }
    }
}
#endif
