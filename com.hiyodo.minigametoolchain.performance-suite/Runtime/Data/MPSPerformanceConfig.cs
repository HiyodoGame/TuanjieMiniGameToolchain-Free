using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniGame.PerformanceSuite.Runtime.Data
{
    /// <summary>
    /// 性能面板配置资源。
    /// </summary>
    [CreateAssetMenu(fileName = "MPSPerformanceConfig", menuName = "MiniGame/Performance Suite/Performance Config")]
    public class MPSPerformanceConfig : ScriptableObject
    {
        [Header("显示")]
        [Tooltip("是否通过 RuntimeInitializeOnLoadMethod 自动创建 HUD")]
        public bool AutoInitialize = true;

        [Tooltip("启动时是否自动显示面板")]
        public bool VisibleOnStart = false;

        [Tooltip("默认热键，按下后切换显示/隐藏")]
        public KeyCode ToggleKey = KeyCode.F12;

        [Tooltip("面板缩放系数")]
        [Range(0.5f, 2f)]
        public float Scale = 1f;

        [Tooltip("面板在屏幕上的默认锚点位置")]
        public Vector2 DefaultAnchor = new Vector2(0.02f, 0.98f);

        [Header("采样")]
        [Tooltip("每秒采样次数")]
        [Range(1, 60)]
        public int SamplesPerSecond = 10;

        [Tooltip("历史数据最大保存秒数")]
        [Range(60, 600)]
        public int MaxHistorySeconds = 300;

        [Tooltip("是否在微信小游戏环境采集 JS Heap")]
        public bool CaptureJsHeap = true;

        [Header("阈值告警")]
        [Tooltip("FPS 低于该值标红")]
        public float FpsWarningThreshold = 30f;

        [Tooltip("Unity Heap 高于该值标红（MB）")]
        public float UnityHeapWarningThresholdMB = 800f;

        [Tooltip("JS Heap 高于该值标红（MB）")]
        public float JsHeapWarningThresholdMB = 400f;

        [Tooltip("每帧 GC Alloc 高于该值标红（KB）")]
        public float GcAllocWarningThresholdKB = 50f;

        [Header("高级")]
        [Tooltip("uGUI 不可用时是否自动降级到 IMGUI")]
        public bool AutoFallbackToIMGUI = true;

        [Tooltip("是否允许拖拽面板")]
        public bool Draggable = true;

        [Tooltip("导出历史数据时的文件前缀")]
        public string ExportFilePrefix = "mps_performance";

        [Header("启动耗时")]
        [Tooltip("是否自动记录默认启动阶段")]
        public bool ProfileStartup = true;

        [Tooltip("是否自动追踪场景/AssetBundle/Resources 等资源加载事件（用于资源加载瀑布图）")]
        public bool TrackResourceLoads = true;

        [Tooltip("是否在控制台输出每个根阶段完成日志")]
        public bool LogStartupReport = true;

        [Tooltip("默认启动阶段警告阈值（秒）")]
        public float DefaultStartupStageThreshold = 1f;

        [Tooltip("各启动阶段警告阈值")]
        public List<MPSStartupStageThreshold> StartupStageThresholds = new List<MPSStartupStageThreshold>
        {
            new MPSStartupStageThreshold { StageName = "FirstFrame", ThresholdSeconds = 0.5f },
            new MPSStartupStageThreshold { StageName = "FirstSceneLoad", ThresholdSeconds = 2f },
            new MPSStartupStageThreshold { StageName = "AssetWarmUp", ThresholdSeconds = 3f },
            new MPSStartupStageThreshold { StageName = "WeChatApiInit", ThresholdSeconds = 1f },
            new MPSStartupStageThreshold { StageName = "ShaderWarmUp", ThresholdSeconds = 2f }
        };

        [Header("内存快照")]
        [Tooltip("是否启用 Heap 超阈值自动快照")]
        public bool AutoMemorySnapshot = true;

        [Tooltip("Unity Heap 超过该值（MB）时自动触发快照")]
        public float UnityHeapSnapshotThresholdMB = 500f;

        [Tooltip("JS Heap 超过该值（MB）时自动触发快照")]
        public float JsHeapSnapshotThresholdMB = 200f;

        [Tooltip("自动快照冷却时间（秒）")]
        public float MemorySnapshotCooldownSeconds = 10f;

        [Tooltip("最大保存快照数量")]
        [Range(5, 100)]
        public int MaxMemorySnapshots = 20;

        [Header("HUD 增强")]
        [Tooltip("是否在 HUD 上显示历史曲线图")]
        public bool ShowHistoryGraph = true;

        [Tooltip("历史曲线图最大数据点数")]
        [Range(30, 300)]
        public int GraphMaxPoints = 60;

        [Tooltip("是否在 HUD 上显示启动耗时汇总")]
        public bool ShowStartupSummary = true;

        [Tooltip("是否在历史曲线图上标记 GC 触发事件（竖线）")]
        public bool MarkGcEvents = true;

        [Tooltip("是否周期性估算音频内存（AudioClip 总占用）")]
        public bool CaptureAudioMemory = true;

        [Tooltip("音频内存扫描间隔（秒）")]
        [Range(1f, 60f)]
        public float AudioMemoryScanIntervalSeconds = 5f;

        [Tooltip("音频内存高于该值标红（MB）")]
        public float AudioMemoryWarningThresholdMB = 100f;

        [Header("内存泄漏检测")]
        [Tooltip("是否启用内存泄漏检测")]
        public bool EnableLeakDetection = true;

        [Tooltip("泄漏检测窗口大小（快照数量）")]
        [Range(3, 50)]
        public int LeakDetectionWindowSize = 10;

        [Tooltip("每次分析间隔（秒）")]
        public float LeakAnalysisIntervalSeconds = 30f;

        [Tooltip("判定泄漏的最小增长量（MB）")]
        public float LeakGrowthThresholdMB = 50f;

        [Tooltip("判定泄漏的最小增长百分比")]
        [Range(1f, 100f)]
        public float LeakGrowthThresholdPercent = 10f;

        [Tooltip("检测到泄漏时是否在控制台输出警告")]
        public bool LogLeakWarning = true;

        [Tooltip("检测到泄漏时是否在 HUD 上显示告警")]
        public bool ShowLeakWarningOnHud = true;

        [Header("告警通道")]
        [Tooltip("是否启用告警系统")]
        public bool EnableAlerts = true;

        [Tooltip("告警通道（按位组合：1=Console, 2=FileReport, 4=EditorDialog）")]
        public int AlertChannels = 1; // Console

        [Tooltip("文件报告路径（启用 FileReport 时生效）")]
        public string AlertReportPath = "mps_alerts.log";

        [Tooltip("最大保存告警历史条数")]
        public int MaxAlertHistory = 100;

        [Tooltip("FPS 低告警冷却时间（秒）")]
        public float FpsAlertCooldownSeconds = 5f;

        [Tooltip("Heap 高告警冷却时间（秒）")]
        public float HeapAlertCooldownSeconds = 10f;
    }

    /// <summary>
    /// 启动阶段阈值配置项。
    /// </summary>
    [Serializable]
    public class MPSStartupStageThreshold
    {
        public string StageName;
        public float ThresholdSeconds;
    }
}
