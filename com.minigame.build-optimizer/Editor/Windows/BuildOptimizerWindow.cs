using System.Collections.Generic;
using System.IO;
using System.Linq;
using MiniGame.Core.Editor.Analyzers;
using MiniGame.Core.Editor.ReportGenerators;
using MiniGame.Core.Editor.SettingsManager;
using MiniGame.Core.Editor.Licensing;
using MiniGame.BuildOptimizer.Editor.Analyzers;
using UnityEditor;
using UnityEngine;

namespace MiniGame.BuildOptimizer.Editor.Windows
{
    /// <summary>
    /// Build Optimizer 主窗口。
    /// </summary>
    public class BuildOptimizerWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private DiagnosticReport _report;
        private bool _isScanning;
        private int _selectedPresetIndex;
        private string _selectedSnapshotId;
        private List<string> _snapshotIds = new List<string>();

        [MenuItem("Window/MiniGame/Build Optimizer/Diagnostics Window")]
        public static void ShowWindow()
        {
            var window = GetWindow<BuildOptimizerWindow>("Build Optimizer");
            window.minSize = new Vector2(700, 500);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshSnapshots();
        }

        private void OnGUI()
        {
            MiniGameLicenseStatusBar.Draw();
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("MiniGame Build Optimizer", EditorStyles.largeLabel);
            EditorGUILayout.LabelField("微信小游戏构建诊断与优化工具", EditorStyles.miniLabel);
            EditorGUILayout.Space(10);

            DrawDiagnosticsSection();
            EditorGUILayout.Space(10);
            DrawOptimizationSection();
            EditorGUILayout.Space(10);
            DrawRestoreSection();
            EditorGUILayout.Space(10);

            if (_report != null)
            {
                DrawReport();
            }
        }

        private void DrawDiagnosticsSection()
        {
            EditorGUILayout.LabelField("诊断", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(_isScanning))
            {
                if (GUILayout.Button("开始诊断", GUILayout.Height(32)))
                {
                    RunDiagnostics();
                }
            }

            if (_isScanning)
            {
                EditorGUILayout.HelpBox("正在扫描项目资源，请稍候...", MessageType.Info);
            }
        }

        private void DrawOptimizationSection()
        {
            EditorGUILayout.LabelField("一键优化", EditorStyles.boldLabel);

            var optimizeEnabled = MiniGameLicenseManager.IsFeatureEnabled(MiniGameLicenseFeature.BuildOptimizerOneClickOptimize);
            if (!optimizeEnabled)
            {
                EditorGUILayout.HelpBox(MiniGameLicenseManager.GetUpgradeHint(MiniGameLicenseFeature.BuildOptimizerOneClickOptimize), MessageType.Warning);
            }

            using (new EditorGUI.DisabledScope(!optimizeEnabled))
            {
            }

            EditorGUILayout.HelpBox("应用预设前会自动创建 Settings 快照，可在下方回滚。", MessageType.Info);
        }

        private void DrawRestoreSection()
        {
            EditorGUILayout.LabelField("回滚", EditorStyles.boldLabel);

            if (_snapshotIds.Count == 0)
            {
                EditorGUILayout.HelpBox("暂无快照。", MessageType.Info);
                return;
            }

            var options = _snapshotIds.ToArray();
            var selectedIndex = Mathf.Max(0, _snapshotIds.IndexOf(_selectedSnapshotId));
            selectedIndex = EditorGUILayout.Popup("选择快照", selectedIndex, options);
            _selectedSnapshotId = options[selectedIndex];

            if (GUILayout.Button("回滚到选中快照"))
            {
                var snapshot = SettingsSnapshot.Load(_selectedSnapshotId);
                if (snapshot != null)
                {
                    if (EditorUtility.DisplayDialog("确认回滚", "这将恢复项目设置到快照时刻，是否继续？", "回滚", "取消"))
                    {
                        snapshot.Restore();
                    }
                }
                else
                {
                    Debug.LogError($"[BuildOptimizer] Failed to load snapshot {_selectedSnapshotId}");
                }
            }
        }

        private void DrawReport()
        {
            EditorGUILayout.LabelField($"综合评分: {_report.OverallScore}/100", EditorStyles.boldLabel);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("类别汇总", EditorStyles.boldLabel);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            foreach (var category in _report.Categories.Values.OrderByDescending(c => c.TotalPotentialSavingsBytes))
            {
                EditorGUILayout.BeginHorizontal("box");
                EditorGUILayout.LabelField(category.Name, GUILayout.Width(120));
                EditorGUILayout.LabelField($"问题: {category.IssueCount}", GUILayout.Width(80));
                EditorGUILayout.LabelField($"可节省: {FormatBytes(category.TotalPotentialSavingsBytes)}");
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("问题详情", EditorStyles.boldLabel);

            foreach (var issue in _report.Issues)
            {
                var color = issue.Severity == IssueSeverity.Error ? new Color(1f, 0.4f, 0.4f)
                    : issue.Severity == IssueSeverity.Warning ? new Color(1f, 0.8f, 0.2f)
                    : Color.white;

                var previousColor = GUI.color;
                GUI.color = color;
                EditorGUILayout.BeginVertical("box");
                GUI.color = previousColor;

                EditorGUILayout.LabelField($"[{issue.Severity}] {issue.Title}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(issue.Description, EditorStyles.wordWrappedLabel);
                EditorGUILayout.LabelField($"资产: {issue.AssetPath}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"建议: {issue.SuggestedFix}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"预估节省: {FormatBytes(issue.PotentialSavingsBytes)}", EditorStyles.miniLabel);

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);
            if (GUILayout.Button("导出 Markdown 报告"))
            {
                var path = EditorUtility.SaveFilePanel("导出报告", "", "BuildOptimizerReport", "md");
                if (!string.IsNullOrEmpty(path))
                {
                    ReportGenerator.ExportMarkdown(_report, path);
                    Debug.Log($"[BuildOptimizer] Report exported to {path}");
                }
            }

            if (GUILayout.Button("导出 HTML 报告"))
            {
                var path = EditorUtility.SaveFilePanel("导出报告", "", "BuildOptimizerReport", "html");
                if (!string.IsNullOrEmpty(path))
                {
                    ReportGenerator.ExportHtml(_report, path);
                    Debug.Log($"[BuildOptimizer] Report exported to {path}");
                }
            }
        }

        private void RunDiagnostics()
        {
            _isScanning = true;

            try
            {
                var engine = new DiagnosticRuleEngine();
                engine.RegisterRule(new TextureDiagnosticRule());
                engine.RegisterRule(new ShaderDiagnosticRule());
                engine.RegisterRule(new FontDiagnosticRule());
                engine.RegisterRule(new SettingsDiagnosticRule());
                engine.RegisterRule(new UnusedAssetDiagnosticRule());

                var context = new DiagnosticContext();
                var issues = engine.RunAll(context);
                _report = ReportGenerator.GenerateReport(issues);
            }
            finally
            {
                _isScanning = false;
            }
        }

        private void RefreshSnapshots()
        {
            _snapshotIds = SettingsSnapshot.ListSnapshotIds();
            if (!string.IsNullOrEmpty(_selectedSnapshotId) && !_snapshotIds.Contains(_selectedSnapshotId))
            {
                _selectedSnapshotId = _snapshotIds.FirstOrDefault();
            }
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024f:F2} KB";
            return $"{bytes / (1024f * 1024f):F2} MB";
        }
    }
}
