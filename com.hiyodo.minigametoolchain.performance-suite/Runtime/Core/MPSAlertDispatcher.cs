using System;
using System.Collections.Generic;
using System.IO;
using MiniGame.PerformanceSuite.Runtime.Data;
using UnityEngine;

namespace MiniGame.PerformanceSuite.Runtime.Core
{
    /// <summary>
    /// Performance Suite 告警分发器。支持控制台、文件报告、编辑器弹窗等通道。
    /// </summary>
    public static class MPSAlertDispatcher
    {
        [Flags]
        public enum Channel
        {
            Console = 1,
            FileReport = 2,
            EditorDialog = 4
        }

        private static MPSPerformanceConfig _config;
        private static readonly List<MPSAlertMessage> _history = new List<MPSAlertMessage>();

        /// <summary>
        /// 已发出的告警历史。
        /// </summary>
        public static IReadOnlyList<MPSAlertMessage> History => _history;

        /// <summary>
        /// 初始化告警分发器。
        /// </summary>
        public static void Initialize(MPSPerformanceConfig config)
        {
            _config = config;
        }

        /// <summary>
        /// 发送告警。
        /// </summary>
        public static void Dispatch(MPSAlertMessage message)
        {
            if (_config == null || !_config.EnableAlerts) return;

            _history.Add(message);
            while (_history.Count > (_config.MaxAlertHistory > 0 ? _config.MaxAlertHistory : 100))
            {
                _history.RemoveAt(0);
            }

            var channels = (Channel)_config.AlertChannels;

            if ((channels & Channel.Console) != 0)
            {
                DispatchToConsole(message);
            }

            if ((channels & Channel.FileReport) != 0)
            {
                DispatchToFile(message);
            }

            if ((channels & Channel.EditorDialog) != 0)
            {
                DispatchToEditorDialog(message);
            }
        }

        /// <summary>
        /// 发送 FPS 低告警。
        /// </summary>
        public static void AlertFps(float fps, float threshold)
        {
            Dispatch(new MPSAlertMessage(
                "FPS 过低",
                $"当前 FPS {fps:F1} 低于阈值 {threshold:F1}",
                MPSAlertLevel.Warning,
                "FPS"
            ));
        }

        /// <summary>
        /// 发送 Unity Heap 高告警。
        /// </summary>
        public static void AlertUnityHeap(long bytes, float thresholdMB)
        {
            Dispatch(new MPSAlertMessage(
                "Unity Heap 过高",
                $"当前 Unity Heap {FormatBytes(bytes)} 高于阈值 {thresholdMB:F0} MB",
                MPSAlertLevel.Warning,
                "UnityHeap"
            ));
        }

        /// <summary>
        /// 发送 JS Heap 高告警。
        /// </summary>
        public static void AlertJsHeap(long bytes, float thresholdMB)
        {
            Dispatch(new MPSAlertMessage(
                "JS Heap 过高",
                $"当前 JS Heap {FormatBytes(bytes)} 高于阈值 {thresholdMB:F0} MB",
                MPSAlertLevel.Warning,
                "JsHeap"
            ));
        }

        /// <summary>
        /// 发送内存泄漏告警。
        /// </summary>
        public static void AlertMemoryLeak(string leakType, long growthBytes, float growthPercent)
        {
            Dispatch(new MPSAlertMessage(
                $"检测到 {leakType} 内存泄漏",
                $"增长 {FormatBytes(growthBytes)} ({growthPercent:F1}%)",
                MPSAlertLevel.Error,
                "Leak"
            ));
        }

        private static void DispatchToConsole(MPSAlertMessage message)
        {
            string text = $"[MPS Alert][{message.Category}] {message.Title}: {message.Body}";
            switch (message.Level)
            {
                case MPSAlertLevel.Error:
                    Debug.LogError(text);
                    break;
                case MPSAlertLevel.Warning:
                    Debug.LogWarning(text);
                    break;
                default:
                    Debug.Log(text);
                    break;
            }
        }

        private static void DispatchToFile(MPSAlertMessage message)
        {
            if (string.IsNullOrEmpty(_config.AlertReportPath)) return;

            try
            {
                string line = $"[{message.Timestamp}] [{message.Level}] [{message.Category}] {message.Title}: {message.Body}{Environment.NewLine}";
                File.AppendAllText(_config.AlertReportPath, line);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MPSAlertDispatcher] Failed to write alert report: {ex.Message}");
            }
        }

        private static void DispatchToEditorDialog(MPSAlertMessage message)
        {
#if UNITY_EDITOR
            if (message.Level == MPSAlertLevel.Error)
            {
                UnityEditor.EditorUtility.DisplayDialog($"[MPS] {message.Title}", message.Body, "OK");
            }
#endif
        }

        private static string FormatBytes(long bytes)
        {
            if (Mathf.Abs(bytes) < 1024) return $"{bytes} B";
            if (Mathf.Abs(bytes) < 1024 * 1024) return $"{bytes / 1024f:F1} KB";
            return $"{bytes / (1024f * 1024f):F1} MB";
        }
    }
}
