using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MiniGame.Core.Editor.Analyzers
{
    /// <summary>
    /// 问题严重级别。
    /// </summary>
    public enum IssueSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2
    }

    /// <summary>
    /// 单一诊断问题。
    /// </summary>
    [Serializable]
    public class DiagnosticIssue
    {
        public string RuleId;
        public string Category;
        public IssueSeverity Severity;
        public string Title;
        public string Description;
        public string AssetPath;
        public string AssetGuid;
        public long PotentialSavingsBytes;
        public string SuggestedFix;
        public bool AutoFixable;

        /// <summary>
        /// 修复命令的稳定标识，例如 "Settings.IL2CPP"、"Texture.Compress"。
        /// </summary>
        public string FixKey;

        /// <summary>
        /// 修复命令的辅助参数，例如目标尺寸、格式等。
        /// </summary>
        public string FixData;
    }

    /// <summary>
    /// 诊断上下文，包含当前项目信息和已扫描数据。
    /// </summary>
    public class DiagnosticContext
    {
        public string TargetPlatform = "WeChatMiniGame";
        public List<string> BuildScenePaths = new List<string>();
        public Dictionary<string, object> Cache = new Dictionary<string, object>();
    }

    /// <summary>
    /// 诊断规则基类。所有扫描器（纹理、Shader、字体等）继承此类。
    /// </summary>
    public abstract class DiagnosticRule
    {
        public string RuleId;
        public string Category;
        public IssueSeverity DefaultSeverity;
        public string DisplayName;

        /// <summary>
        /// 执行诊断并返回问题列表。
        /// </summary>
        public abstract List<DiagnosticIssue> Evaluate(DiagnosticContext context);
    }

    /// <summary>
    /// 规则引擎：自动发现、排序并执行所有 DiagnosticRule 子类。
    /// </summary>
    public class DiagnosticRuleEngine
    {
        private readonly List<DiagnosticRule> _rules = new List<DiagnosticRule>();

        /// <summary>
        /// 注册规则实例。
        /// </summary>
        public void RegisterRule(DiagnosticRule rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            _rules.Add(rule);
        }

        /// <summary>
        /// 注册所有指定程序集中继承自 DiagnosticRule 的非抽象类。
        /// </summary>
        public void RegisterAllRules(System.Reflection.Assembly assembly)
        {
            var ruleTypes = assembly.GetTypes()
                .Where(t => typeof(DiagnosticRule).IsAssignableFrom(t)
                            && t.IsClass
                            && !t.IsAbstract);

            foreach (var type in ruleTypes)
            {
                try
                {
                    var rule = (DiagnosticRule)Activator.CreateInstance(type);
                    RegisterRule(rule);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[MiniGame.Core] Failed to instantiate rule {type.Name}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 执行所有已注册规则。
        /// </summary>
        /// <param name="context">诊断上下文。</param>
        /// <param name="progress">进度回调，返回 false 表示用户取消。</param>
        public List<DiagnosticIssue> RunAll(DiagnosticContext context, System.Func<string, int, int, bool> progress = null)
        {
            var results = new List<DiagnosticIssue>();
            var orderedRules = _rules.OrderByDescending(r => r.DefaultSeverity).ToList();

            for (int i = 0; i < orderedRules.Count; i++)
            {
                var rule = orderedRules[i];
                if (progress != null && !progress(rule.DisplayName ?? rule.RuleId, i + 1, orderedRules.Count))
                {
                    Debug.Log("[MiniGame.Core] 诊断已取消。");
                    break;
                }

                try
                {
                    var issues = rule.Evaluate(context);
                    if (issues != null)
                    {
                        foreach (var issue in issues)
                        {
                            issue.RuleId = issue.RuleId ?? rule.RuleId;
                            issue.Category = issue.Category ?? rule.Category;
                        }
                        results.AddRange(issues);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[MiniGame.Core] Rule {rule.RuleId} failed: {ex}");
                }
            }

            return results
                .OrderByDescending(i => i.Severity)
                .ThenByDescending(i => i.PotentialSavingsBytes)
                .ToList();
        }

        /// <summary>
        /// 获取已注册规则列表（只读）。
        /// </summary>
        public IReadOnlyList<DiagnosticRule> Rules => _rules;
    }
}
