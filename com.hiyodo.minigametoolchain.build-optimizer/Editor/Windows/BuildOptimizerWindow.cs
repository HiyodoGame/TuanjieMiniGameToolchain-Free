using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MiniGame.Core.Editor.Analyzers;
using MiniGame.Core.Editor.ReportGenerators;
using MiniGame.Core.Editor.SettingsManager;
using MiniGame.Core.Editor;
using MiniGame.Core.Editor.Licensing;
using MiniGame.BuildOptimizer.Editor.Analyzers;
using MiniGame.BuildOptimizer.Editor.BuildHistory;
using MiniGame.BuildOptimizer.Editor.ReportGenerators;
using UnityEditor;
using UnityEngine;

namespace MiniGame.BuildOptimizer.Editor.Windows
{
    /// <summary>
    /// Build Optimizer 主窗口（新版布局：左侧导航 + 主内容区）。
    /// </summary>
    public class BuildOptimizerWindow : EditorWindow
    {
        private enum ViewTab
        {
            Dashboard,
            Issues,
            History,
            Optimize,
            Pro
        }

        private static readonly string[] TabLabels = { "概览", "问题", "历史", "优化", "Pro" };
        private static readonly string[] SeverityFilterLabels = { "全部", "Error", "Warning", "Info" };
        private static readonly string[] SortLabels = { "严重级别", "预估节省", "类别" };

        private int _selectedTab;
        private Vector2 _mainScrollPosition;
        private Vector2 _sidebarScrollPosition;

        private DiagnosticReport _report;
        private bool _isScanning;
        private float _progress;
        private string _progressMessage;

        // Optimize tab state
        private int _selectedPresetIndex;
        private string _selectedSnapshotId;
        private List<string> _snapshotIds = new List<string>();
        private string _snapshotComment = "";
        private bool _rerunDiagnosticsAfterRestore;

        // Pro tab state
        private int _selectedProSubTab;
        private static readonly string[] ProSubTabLabels = { "分包", "字体", "Shader" };
        private string _bundleRemoteLoadPath = "https://your-cdn-domain.com/[BuildTarget]";
        private BundleStrategy _lastBundleStrategy;
        private string _bundleReportPath = "Logs/BuildOptimizer/BundleReports";

        // History tab state
        private int _selectedBaselineIndex;
        private int _selectedCurrentIndex;
        private List<string> _historyIds = new List<string>();
        private BuildHistoryComparison _comparison;
        private Vector2 _historyScrollPosition;
        private bool _autoSaveHistory;
        private const string AutoSaveHistoryKey = "MiniGame_BuildOptimizer_AutoSaveHistory";

        // Issues tab state
        private int _severityFilterIndex;
        private int _categoryFilterIndex;
        private int _sortModeIndex;
        private string _searchText = "";
        private bool[] _categoryFoldouts;

        [MenuItem("Window/MiniGame/Build Optimizer/Diagnostics Window")]
        public static void ShowWindow()
        {
            var window = GetWindow<BuildOptimizerWindow>("Build Optimizer");
            window.minSize = new Vector2(860, 560);
            MiniGameBranding.SetTitleIcon(window, "Build Optimizer");
            window.Show();
        }

        private void OnEnable()
        {
            RefreshSnapshots();
            RefreshHistoryList();
            _autoSaveHistory = EditorPrefs.GetBool(AutoSaveHistoryKey, true);
        }

        private void OnGUI()
        {
            // 顶部品牌头与状态栏
            MiniGameBranding.DrawHeader("MiniGame Build Optimizer", "微信小游戏构建诊断与优化工具");
            MiniGameLicenseStatusBar.Draw();
            EditorGUILayout.Space(8);

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawSidebar(GUILayout.Width(150));
                DrawMainContent();
            }
        }

        #region Sidebar

        private void DrawSidebar(GUILayoutOption widthOption)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, widthOption))
            {
                _sidebarScrollPosition = EditorGUILayout.BeginScrollView(_sidebarScrollPosition, GUIStyle.none, GUI.skin.verticalScrollbar);

                EditorGUILayout.LabelField("导航", EditorStyles.boldLabel);
                EditorGUILayout.Space(4);

                var oldTab = _selectedTab;
                _selectedTab = GUILayout.SelectionGrid(_selectedTab, TabLabels, 1, GUILayout.ExpandWidth(true));
                if (oldTab != _selectedTab)
                {
                    _mainScrollPosition = Vector2.zero;
                    GUI.FocusControl(null);
                }

                EditorGUILayout.Space(16);

                // 快捷诊断按钮
                using (new EditorGUI.DisabledScope(_isScanning))
                {
                    if (GUILayout.Button("开始诊断", GUILayout.Height(36)))
                    {
                        RunDiagnostics();
                    }
                }

                if (_isScanning)
                {
                    EditorGUILayout.Space(4);
                    EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(false, 16f), _progress, $"{_progress:0%}");
                    EditorGUILayout.LabelField(_progressMessage, EditorStyles.miniLabel);
                }

                EditorGUILayout.Space(16);
                DrawOverallScoreMini();

                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawOverallScoreMini()
        {
            if (_report == null) return;

            EditorGUILayout.LabelField("当前评分", EditorStyles.boldLabel);
            var scoreColor = GetScoreColor(_report.OverallScore);
            var prevColor = GUI.color;
            GUI.color = scoreColor;
            EditorGUILayout.LabelField($"{_report.OverallScore}/100", new GUIStyle(EditorStyles.largeLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 24, fontStyle = FontStyle.Bold });
            GUI.color = prevColor;
            EditorGUILayout.LabelField($"问题 {_report.Issues.Count}", EditorStyles.miniLabel);
        }

        #endregion

        #region Main Content

        private void DrawMainContent()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _mainScrollPosition = EditorGUILayout.BeginScrollView(_mainScrollPosition);

                switch ((ViewTab)_selectedTab)
                {
                    case ViewTab.Dashboard:
                        DrawDashboard();
                        break;
                    case ViewTab.Issues:
                        DrawIssues();
                        break;
                    case ViewTab.History:
                        DrawHistory();
                        break;
                    case ViewTab.Optimize:
                        DrawOptimize();
                        break;
                    case ViewTab.Pro:
                        DrawPro();
                        break;
                }

                EditorGUILayout.EndScrollView();
            }
        }

        #endregion

        #region Dashboard

        private void DrawDashboard()
        {
            EditorGUILayout.LabelField("概览", EditorStyles.boldLabel);
            EditorGUILayout.Space(8);

            if (_report == null)
            {
                EditorGUILayout.HelpBox("点击左侧「开始诊断」运行首次扫描，查看项目构建健康度。", MessageType.Info);
                return;
            }

            DrawScoreCard();
            EditorGUILayout.Space(16);
            DrawCategorySummaryChart();
            EditorGUILayout.Space(16);
            DrawTopIssuesPreview();
        }

        private void DrawScoreCard()
        {
            using (new EditorGUILayout.HorizontalScope("box"))
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(160)))
                {
                    EditorGUILayout.LabelField("综合评分", EditorStyles.boldLabel);
                    var scoreColor = GetScoreColor(_report.OverallScore);
                    var prevColor = GUI.color;
                    GUI.color = scoreColor;
                    EditorGUILayout.LabelField($"{_report.OverallScore}", new GUIStyle(EditorStyles.largeLabel) { fontSize = 48, fontStyle = FontStyle.Bold });
                    GUI.color = prevColor;
                    EditorGUILayout.LabelField("/ 100", EditorStyles.miniLabel);
                }

                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField($"生成时间: {_report.GeneratedAt:yyyy-MM-dd HH:mm}", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField($"引擎版本: {_report.EngineVersion}", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField($"目标平台: {_report.TargetPlatform}", EditorStyles.miniLabel);
                    EditorGUILayout.Space(8);

                    var errorCount = _report.Issues.Count(i => i.Severity == IssueSeverity.Error);
                    var warningCount = _report.Issues.Count(i => i.Severity == IssueSeverity.Warning);
                    var infoCount = _report.Issues.Count(i => i.Severity == IssueSeverity.Info);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        DrawCountBadge("Error", errorCount, new Color(1f, 0.35f, 0.35f));
                        DrawCountBadge("Warning", warningCount, new Color(1f, 0.75f, 0.15f));
                        DrawCountBadge("Info", infoCount, new Color(0.35f, 0.65f, 1f));
                    }
                }
            }
        }

        private void DrawCountBadge(string label, int count, Color color)
        {
            using (new EditorGUILayout.HorizontalScope("box", GUILayout.Width(110)))
            {
                var prev = GUI.color;
                GUI.color = color;
                EditorGUILayout.LabelField(count.ToString(), new GUIStyle(EditorStyles.boldLabel) { fontSize = 18, alignment = TextAnchor.MiddleCenter }, GUILayout.Width(40));
                GUI.color = prev;
                EditorGUILayout.LabelField(label, EditorStyles.miniLabel);
            }
        }

        private void DrawCategorySummaryChart()
        {
            EditorGUILayout.LabelField("分类汇总", EditorStyles.boldLabel);

            var categories = _report.Categories.Values.OrderByDescending(c => c.TotalPotentialSavingsBytes).ToList();
            if (categories.Count == 0)
            {
                EditorGUILayout.HelpBox("暂无分类数据。", MessageType.Info);
                return;
            }

            long maxSavings = categories.Max(c => c.TotalPotentialSavingsBytes);
            if (maxSavings <= 0) maxSavings = 1;

            foreach (var category in categories)
            {
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(category.Name, GUILayout.Width(110));
                        EditorGUILayout.LabelField($"问题 {category.IssueCount}", GUILayout.Width(70));
                        EditorGUILayout.LabelField($"评分 {category.Score}", GUILayout.Width(60));
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.LabelField(FormatBytes(category.TotalPotentialSavingsBytes), GUILayout.Width(80));
                    }

                    var ratio = Mathf.Clamp01((float)category.TotalPotentialSavingsBytes / maxSavings);
                    EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(false, 14f), ratio, $"{ratio:0%}");
                }
            }
        }

        private void DrawTopIssuesPreview()
        {
            EditorGUILayout.LabelField("重点关注", EditorStyles.boldLabel);

            var topIssues = _report.Issues
                .OrderByDescending(i => i.Severity)
                .ThenByDescending(i => i.PotentialSavingsBytes)
                .Take(5)
                .ToList();

            if (topIssues.Count == 0)
            {
                EditorGUILayout.HelpBox("当前没有检测到问题。", MessageType.Info);
                return;
            }

            foreach (var issue in topIssues)
            {
                DrawIssueCard(issue, false);
            }
        }

        #endregion

        #region Issues

        private void DrawIssues()
        {
            EditorGUILayout.LabelField("问题详情", EditorStyles.boldLabel);

            if (_report == null)
            {
                EditorGUILayout.HelpBox("请先运行诊断。", MessageType.Info);
                return;
            }

            DrawIssuesToolbar();
            EditorGUILayout.Space(8);

            var filtered = GetFilteredAndSortedIssues();
            if (filtered.Count == 0)
            {
                EditorGUILayout.HelpBox("没有符合条件的问题。", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"共 {filtered.Count} 条问题", EditorStyles.miniLabel);

            foreach (var issue in filtered)
            {
                DrawIssueCard(issue, true);
            }

            EditorGUILayout.Space(10);
            DrawExportButtons();
        }

        private void DrawIssuesToolbar()
        {
            using (new EditorGUILayout.HorizontalScope("box"))
            {
                EditorGUILayout.LabelField("筛选", EditorStyles.boldLabel, GUILayout.Width(40));

                _severityFilterIndex = EditorGUILayout.Popup("级别", _severityFilterIndex, SeverityFilterLabels, GUILayout.Width(140));

                var categories = new List<string> { "全部" };
                categories.AddRange(_report.Categories.Keys.OrderBy(k => k));
                _categoryFilterIndex = EditorGUILayout.Popup("类别", _categoryFilterIndex, categories.ToArray(), GUILayout.Width(160));

                _sortModeIndex = EditorGUILayout.Popup("排序", _sortModeIndex, SortLabels, GUILayout.Width(120));

                GUILayout.FlexibleSpace();

                _searchText = EditorGUILayout.TextField(_searchText, GUI.skin.FindStyle("ToolbarSeachTextField") ?? EditorStyles.textField, GUILayout.Width(180));
                if (GUILayout.Button("×", GUILayout.Width(24)))
                {
                    _searchText = "";
                    GUI.FocusControl(null);
                }
            }
        }

        private List<DiagnosticIssue> GetFilteredAndSortedIssues()
        {
            var query = _report.Issues.AsEnumerable();

            // 严重级别筛选
            if (_severityFilterIndex == 1) query = query.Where(i => i.Severity == IssueSeverity.Error);
            else if (_severityFilterIndex == 2) query = query.Where(i => i.Severity == IssueSeverity.Warning);
            else if (_severityFilterIndex == 3) query = query.Where(i => i.Severity == IssueSeverity.Info);

            // 类别筛选
            var categories = _report.Categories.Keys.OrderBy(k => k).ToList();
            if (_categoryFilterIndex > 0 && _categoryFilterIndex - 1 < categories.Count)
            {
                var selectedCategory = categories[_categoryFilterIndex - 1];
                query = query.Where(i => (i.Category ?? "General") == selectedCategory);
            }

            // 搜索
            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                var lower = _searchText.ToLowerInvariant();
                query = query.Where(i =>
                    (i.Title?.ToLowerInvariant().Contains(lower) ?? false) ||
                    (i.Description?.ToLowerInvariant().Contains(lower) ?? false) ||
                    (i.AssetPath?.ToLowerInvariant().Contains(lower) ?? false));
            }

            // 排序
            query = _sortModeIndex switch
            {
                0 => query.OrderByDescending(i => i.Severity).ThenByDescending(i => i.PotentialSavingsBytes),
                1 => query.OrderByDescending(i => i.PotentialSavingsBytes).ThenByDescending(i => i.Severity),
                2 => query.OrderBy(i => i.Category).ThenByDescending(i => i.Severity),
                _ => query
            };

            return query.ToList();
        }

        private void DrawIssueCard(DiagnosticIssue issue, bool showFixButton)
        {
            var color = issue.Severity == IssueSeverity.Error ? new Color(1f, 0.35f, 0.35f)
                : issue.Severity == IssueSeverity.Warning ? new Color(1f, 0.8f, 0.2f)
                : new Color(0.7f, 0.85f, 1f);

            var prevColor = GUI.color;
            GUI.color = color * 0.25f;
            EditorGUILayout.BeginVertical("box");
            GUI.color = prevColor;

            using (new EditorGUILayout.HorizontalScope())
            {
                var severityStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    normal = { textColor = color },
                    fontSize = 12
                };
                EditorGUILayout.LabelField($"[{issue.Severity}] {issue.Title}", severityStyle, GUILayout.ExpandWidth(true));
                EditorGUILayout.LabelField(FormatBytes(issue.PotentialSavingsBytes), EditorStyles.miniLabel, GUILayout.Width(80));
            }

            EditorGUILayout.LabelField(issue.Description, EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField($"资产: {issue.AssetPath}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"建议: {issue.SuggestedFix}", EditorStyles.miniLabel);

            if (showFixButton && issue.AutoFixable && MiniGameLicenseManager.IsFeatureEnabled(MiniGameLicenseFeature.BuildOptimizerOneClickOptimize))
            {
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawExportButtons()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("导出 Markdown"))
                {
                    var path = EditorUtility.SaveFilePanel("导出报告", "", "BuildOptimizerReport", "md");
                    if (!string.IsNullOrEmpty(path))
                    {
                        ReportGenerator.ExportMarkdown(_report, path);
                        Debug.Log($"[BuildOptimizer] Report exported to {path}");
                    }
                }

                if (GUILayout.Button("导出 HTML"))
                {
                    var path = EditorUtility.SaveFilePanel("导出报告", "", "BuildOptimizerReport", "html");
                    if (!string.IsNullOrEmpty(path))
                    {
                        BuildOptimizerHtmlReport.Export(_report, path);
                        Debug.Log($"[BuildOptimizer] 可视化报告已导出: {path}");
                    }
                }

                if (GUILayout.Button("导出 PDF"))
                {
                    var path = EditorUtility.SaveFilePanel("导出 PDF", "", "BuildOptimizerReport", "pdf");
                    if (!string.IsNullOrEmpty(path))
                    {
                        if (BuildOptimizerPdfReport.Export(_report, path))
                        {
                            Debug.Log($"[BuildOptimizer] PDF 报告已导出: {path}");
                        }
                    }
                }

                if (GUILayout.Button("导出 CSV"))
                {
                    var path = EditorUtility.SaveFilePanel("导出 CSV", "", "BuildOptimizerReport", "csv");
                    if (!string.IsNullOrEmpty(path))
                    {
                        ReportGenerator.ExportCsv(_report, path);
                        Debug.Log($"[BuildOptimizer] CSV 报告已导出: {path}");
                    }
                }
            }
        }

        #endregion

        #region History

        private void DrawHistory()
        {
            EditorGUILayout.LabelField("历史对比", EditorStyles.boldLabel);
            EditorGUILayout.Space(8);

            EditorGUI.BeginChangeCheck();
            _autoSaveHistory = EditorGUILayout.ToggleLeft("诊断完成后自动保存历史", _autoSaveHistory);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool(AutoSaveHistoryKey, _autoSaveHistory);
            }

            if (_report != null)
            {
                if (GUILayout.Button("保存本次诊断结果", GUILayout.Height(28)))
                {
                    BuildHistoryStorage.Save(_report, $"IssueCount:{_report.Issues.Count}");
                    RefreshHistoryList();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("先运行一次诊断，再保存到历史记录。", MessageType.Info);
            }

            RefreshHistoryList();
            if (_historyIds.Count < 2)
            {
                EditorGUILayout.HelpBox("至少需要两条历史记录才能对比。", MessageType.Info);
                return;
            }

            var labels = _historyIds.Select(BuildHistoryStorage.GetLabel).ToArray();
            _selectedBaselineIndex = Mathf.Clamp(_selectedBaselineIndex, 0, labels.Length - 1);
            _selectedCurrentIndex = Mathf.Clamp(_selectedCurrentIndex, 0, labels.Length - 1);

            using (new EditorGUILayout.HorizontalScope("box"))
            {
                _selectedBaselineIndex = EditorGUILayout.Popup("基线", _selectedBaselineIndex, labels);
                _selectedCurrentIndex = EditorGUILayout.Popup("当前", _selectedCurrentIndex, labels);
                if (GUILayout.Button("对比", GUILayout.Width(80)))
                {
                    var baseline = BuildHistoryStorage.Load(_historyIds[_selectedBaselineIndex]);
                    var current = BuildHistoryStorage.Load(_historyIds[_selectedCurrentIndex]);
                    if (baseline != null && current != null)
                    {
                        _comparison = BuildHistoryComparer.Compare(baseline, current);
                    }
                }
            }

            if (_comparison != null)
            {
                DrawComparison(_comparison);
            }
        }

        private void DrawComparison(BuildHistoryComparison comparison)
        {
            _historyScrollPosition = EditorGUILayout.BeginScrollView(_historyScrollPosition, GUILayout.MaxHeight(400));

            EditorGUILayout.LabelField("综合评分", EditorStyles.boldLabel);
            var scoreColor = comparison.ScoreDelta >= 0 ? Color.green : Color.red;
            var prevColor = GUI.color;
            GUI.color = scoreColor;
            EditorGUILayout.LabelField($"基线: {comparison.Baseline.OverallScore} → 当前: {comparison.Current.OverallScore} (Δ {comparison.ScoreDelta:+#;-#;0})");
            GUI.color = prevColor;

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("类别变化", EditorStyles.boldLabel);
            foreach (var delta in comparison.CategoryDeltas.OrderByDescending(d => Math.Abs(d.SavingsBytesDelta)))
            {
                EditorGUILayout.BeginHorizontal("box");
                EditorGUILayout.LabelField(delta.Name, GUILayout.Width(100));
                EditorGUILayout.LabelField($"问题: {delta.BaselineIssueCount} → {delta.CurrentIssueCount} (Δ {delta.IssueCountDelta:+#;-#;0})", GUILayout.Width(170));
                EditorGUILayout.LabelField($"节省: {FormatBytes(delta.BaselineSavingsBytes)} → {FormatBytes(delta.CurrentSavingsBytes)}", GUILayout.Width(220));
                EditorGUILayout.LabelField($"评分: {delta.BaselineScore} → {delta.CurrentScore}", GUILayout.Width(130));
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField($"新增问题 ({comparison.AddedIssues.Count})", EditorStyles.boldLabel);
            foreach (var issue in comparison.AddedIssues)
            {
                DrawIssueDiff(issue, Color.red);
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField($"已修复/消失问题 ({comparison.RemovedIssues.Count})", EditorStyles.boldLabel);
            foreach (var issue in comparison.RemovedIssues)
            {
                DrawIssueDiff(issue, Color.green);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawIssueDiff(IssueDiff issue, Color color)
        {
            var prevColor = GUI.color;
            GUI.color = color;
            EditorGUILayout.BeginVertical("box");
            GUI.color = prevColor;
            EditorGUILayout.LabelField($"[{issue.Severity}] {issue.Title}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"类别: {issue.Category} | 资产: {issue.AssetPath}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"预估节省: {FormatBytes(issue.PotentialSavingsBytes)}", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Optimize

        private void DrawOptimize()
        {
            EditorGUILayout.LabelField("一键优化", EditorStyles.boldLabel);
            EditorGUILayout.Space(8);

            var optimizeEnabled = MiniGameLicenseManager.IsFeatureEnabled(MiniGameLicenseFeature.BuildOptimizerOneClickOptimize);
            if (!optimizeEnabled)
            {
                EditorGUILayout.HelpBox(MiniGameLicenseManager.GetUpgradeHint(MiniGameLicenseFeature.BuildOptimizerOneClickOptimize), MessageType.Warning);
            }

            using (new EditorGUI.DisabledScope(!optimizeEnabled))
            {
            }

            EditorGUILayout.Space(16);
            DrawRestoreSection();
        }

        private void DrawRestoreSection()
        {
            EditorGUILayout.LabelField("设置回滚", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("应用预设或自动修复前会自动创建快照。你也可以手动创建快照，随时回滚到任意历史状态。", MessageType.Info);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("手动创建快照", EditorStyles.boldLabel);
            _snapshotComment = EditorGUILayout.TextField("备注", _snapshotComment);
            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_snapshotComment)))
            {
                if (GUILayout.Button("立即创建快照", GUILayout.Height(24)))
                {
                    var snapshot = SettingsSnapshot.Capture(_snapshotComment.Trim());
                    snapshot.Save();
                    _snapshotComment = "";
                    RefreshSnapshots();
                }
            }

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("已有快照", EditorStyles.boldLabel);

            RefreshSnapshots();
            if (_snapshotIds.Count == 0)
            {
                EditorGUILayout.HelpBox("暂无快照。", MessageType.Info);
                return;
            }

            _rerunDiagnosticsAfterRestore = EditorGUILayout.ToggleLeft("回滚后自动重新诊断", _rerunDiagnosticsAfterRestore);

            foreach (var snapshotId in _snapshotIds)
            {
                DrawSnapshotItem(snapshotId);
            }
        }

        private void DrawSnapshotItem(string snapshotId)
        {
            var snapshot = SettingsSnapshot.Load(snapshotId);
            if (snapshot == null) return;

            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(snapshot.Comment, EditorStyles.boldLabel, GUILayout.ExpandWidth(true));
                    EditorGUILayout.LabelField(FormatSnapshotTime(snapshot.CreatedAt), EditorStyles.miniLabel, GUILayout.Width(120));
                }
                EditorGUILayout.LabelField($"ID: {snapshot.SnapshotId.Substring(0, Mathf.Min(8, snapshot.SnapshotId.Length))}...", EditorStyles.miniLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("回滚", GUILayout.Width(60)))
                    {
                        var message = $"将恢复项目设置到快照：\n{snapshot.Comment}\n（{FormatSnapshotTime(snapshot.CreatedAt)}）";
                        if (EditorUtility.DisplayDialog("确认回滚", message, "回滚", "取消"))
                        {
                            snapshot.Restore();
                            RefreshSnapshots();
                            if (_rerunDiagnosticsAfterRestore)
                            {
                                RunDiagnostics();
                            }
                        }
                    }

                    if (GUILayout.Button("删除", GUILayout.Width(60)))
                    {
                        var message = $"删除快照：\n{snapshot.Comment}\n（{FormatSnapshotTime(snapshot.CreatedAt)}）";
                        if (EditorUtility.DisplayDialog("确认删除", message, "删除", "取消"))
                        {
                            SettingsSnapshot.Delete(snapshotId);
                            RefreshSnapshots();
                        }
                    }
                }
            }
        }

        private static string FormatSnapshotTime(string createdAt)
        {
            if (DateTime.TryParse(createdAt, out var dt))
            {
                return dt.ToString("yyyy-MM-dd HH:mm");
            }
            return createdAt;
        }

        #endregion

        #region Pro

        private void DrawPro()
        {
            EditorGUILayout.LabelField("Build Optimizer Pro", EditorStyles.boldLabel);
            EditorGUILayout.Space(8);

            var smartBundleEnabled = MiniGameLicenseManager.IsFeatureEnabled(MiniGameLicenseFeature.BuildOptimizerSmartBundle);
            var fontSubsetEnabled = MiniGameLicenseManager.IsFeatureEnabled(MiniGameLicenseFeature.BuildOptimizerFontSubset);
            var shaderVariantEnabled = MiniGameLicenseManager.IsFeatureEnabled(MiniGameLicenseFeature.BuildOptimizerShaderVariant);
            var anyProEnabled = smartBundleEnabled || fontSubsetEnabled || shaderVariantEnabled;

            if (!anyProEnabled)
            {
                EditorGUILayout.HelpBox(MiniGameLicenseManager.GetUpgradeHint(MiniGameLicenseFeature.BuildOptimizerSmartBundle), MessageType.Warning);
                return;
            }

            _selectedProSubTab = GUILayout.Toolbar(_selectedProSubTab, ProSubTabLabels);
            EditorGUILayout.Space(8);

            switch (_selectedProSubTab)
            {
                case 0:
                    DrawBundleOptimizer();
                    break;
                case 1:
                    DrawFontSubsetOptimizer();
                    break;
                case 2:
                    DrawShaderVariantOptimizer();
                    break;
            }
        }

        private void DrawBundleOptimizer()
        {
            var enabled = MiniGameLicenseManager.IsFeatureEnabled(MiniGameLicenseFeature.BuildOptimizerSmartBundle);
            if (!enabled)
            {
                EditorGUILayout.HelpBox(MiniGameLicenseManager.GetUpgradeHint(MiniGameLicenseFeature.BuildOptimizerSmartBundle), MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("智能分包策略", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("基于场景依赖图自动划分 FirstPackage / RemoteCDN / Preload，首包预算 4MB。", MessageType.Info);
            EditorGUILayout.Space(8);

            _bundleRemoteLoadPath = EditorGUILayout.TextField("远程加载路径", _bundleRemoteLoadPath);
            _bundleReportPath = EditorGUILayout.TextField("报告输出目录", _bundleReportPath);

            EditorGUILayout.Space(8);
            if (GUILayout.Button("分析并导出报告", GUILayout.Height(32)))
            {
                var report = BundleOptimizer.AnalyzeAndExport(_bundleReportPath, new Progress<string>(msg => Debug.Log(msg)));
                _lastBundleStrategy = report.Strategy;
                Debug.Log($"[BuildOptimizer] 分包报告已导出: {_bundleReportPath}");
            }

            if (_lastBundleStrategy != null)
            {
                EditorGUILayout.Space(12);
                EditorGUILayout.LabelField("分析结果", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"分组数: {_lastBundleStrategy.Groups.Count}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"首包估算: {FormatBytes(_lastBundleStrategy.EstimatedFirstPackageSizeBytes)}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"远程包估算: {FormatBytes(_lastBundleStrategy.EstimatedRemotePackageSizeBytes)}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"预下载估算: {FormatBytes(_lastBundleStrategy.EstimatedPreloadSizeBytes)}", EditorStyles.miniLabel);

                if (_lastBundleStrategy.Warnings.Count > 0)
                {
                    EditorGUILayout.Space(4);
                    foreach (var warning in _lastBundleStrategy.Warnings)
                    {
                        EditorGUILayout.HelpBox(warning, MessageType.Warning);
                    }
                }

                EditorGUILayout.Space(8);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("应用经典 AssetBundle 名称", GUILayout.Height(28)))
                    {
                        if (EditorUtility.DisplayDialog("确认应用", "这将修改所有分包资源的 assetBundleName，是否继续？", "应用", "取消"))
                        {
                            BundleOptimizer.ApplyClassicAssetBundleNames(_lastBundleStrategy, new Progress<string>(msg => Debug.Log(msg)));
                        }
                    }

                    using (new EditorGUI.DisabledScope(!AddressablesConfigGenerator.IsAddressablesAvailable))
                    {
                        if (GUILayout.Button("生成 Addressables 分组", GUILayout.Height(28)))
                        {
                            if (EditorUtility.DisplayDialog("确认应用", "这将创建/覆盖 Addressables 分组，是否继续？", "应用", "取消"))
                            {
                                try
                                {
                                    BundleOptimizer.ApplyAddressablesConfig(_lastBundleStrategy, _bundleRemoteLoadPath, new Progress<string>(msg => Debug.Log(msg)));
                                }
                                catch (Exception ex)
                                {
                                    EditorUtility.DisplayDialog("Addressables 配置失败", ex.Message, "确定");
                                }
                            }
                        }
                    }
                }
                if (!AddressablesConfigGenerator.IsAddressablesAvailable)
                {
                    EditorGUILayout.HelpBox("未检测到 Addressables 包，无法生成分组配置。", MessageType.Info);
                }
            }
        }

        private void DrawFontSubsetOptimizer()
        {
            var enabled = MiniGameLicenseManager.IsFeatureEnabled(MiniGameLicenseFeature.BuildOptimizerFontSubset);
            if (!enabled)
            {
                EditorGUILayout.HelpBox(MiniGameLicenseManager.GetUpgradeHint(MiniGameLicenseFeature.BuildOptimizerFontSubset), MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("字体子集裁剪", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("扫描场景中实际使用的字符，为 oversized 字体生成 TMP Font Asset 子集。", MessageType.Info);

            EditorGUILayout.Space(8);
            EditorGUILayout.HelpBox("功能开发中，敬请期待。", MessageType.Info);
        }

        private void DrawShaderVariantOptimizer()
        {
            var enabled = MiniGameLicenseManager.IsFeatureEnabled(MiniGameLicenseFeature.BuildOptimizerShaderVariant);
            if (!enabled)
            {
                EditorGUILayout.HelpBox(MiniGameLicenseManager.GetUpgradeHint(MiniGameLicenseFeature.BuildOptimizerShaderVariant), MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("Shader 变体管理", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("分析 Shader 变体数量，生成精简的 ShaderVariantCollection 与异步 WarmUp 代码模板。", MessageType.Info);

            EditorGUILayout.Space(8);
            EditorGUILayout.HelpBox("功能开发中，敬请期待。", MessageType.Info);
        }

        #endregion

        #region Helpers

        private void RefreshHistoryList()
        {
            _historyIds = BuildHistoryStorage.ListIds();
        }

        private void RefreshSnapshots()
        {
            _snapshotIds = SettingsSnapshot.ListSnapshotIds();
            if (!string.IsNullOrEmpty(_selectedSnapshotId) && !_snapshotIds.Contains(_selectedSnapshotId))
            {
                _selectedSnapshotId = _snapshotIds.FirstOrDefault();
            }
        }

        private void RunDiagnostics()
        {
            _isScanning = true;

            try
            {
                var rules = new List<DiagnosticRule>
                {
                    new TextureDiagnosticRule(),
                    new ShaderDiagnosticRule(),
                    new FontDiagnosticRule(),
                    new SettingsDiagnosticRule(),
                    new UnusedAssetDiagnosticRule()
                };

                var engine = new DiagnosticRuleEngine();
                foreach (var rule in rules)
                {
                    engine.RegisterRule(rule);
                }

                var context = new DiagnosticContext();
                var issues = engine.RunAll(context, OnRuleProgress);
                _report = ReportGenerator.GenerateReport(issues);

                // 重置筛选索引，避免切换报告后越界
                _categoryFilterIndex = 0;
                _severityFilterIndex = 0;

                if (_autoSaveHistory)
                {
                    BuildHistoryStorage.Save(_report, $"Auto IssueCount:{_report.Issues.Count}");
                    RefreshHistoryList();
                }

                // 默认跳转到 Dashboard 查看结果
                _selectedTab = (int)ViewTab.Dashboard;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                _isScanning = false;
            }
        }

        private bool OnRuleProgress(string ruleName, int current, int total)
        {
            _progress = (float)current / total;
            _progressMessage = $"正在执行: {ruleName} ({current}/{total})";
            Repaint();
            return EditorUtility.DisplayCancelableProgressBar(
                "构建诊断中",
                _progressMessage,
                _progress);
        }

        private static Color GetScoreColor(int score)
        {
            if (score >= 80) return new Color(0.2f, 0.85f, 0.35f);
            if (score >= 60) return new Color(1f, 0.8f, 0.15f);
            return new Color(1f, 0.3f, 0.25f);
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024f:F2} KB";
            return $"{bytes / (1024f * 1024f):F2} MB";
        }

        #endregion
    }
}
