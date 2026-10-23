using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MiniGame.Core.Editor.ReportGenerators;
using UnityEditor;
using UnityEngine;

namespace MiniGame.BuildOptimizer.Editor.BuildHistory
{
    /// <summary>
    /// 诊断历史持久化存储。
    /// </summary>
    public static class BuildHistoryStorage
    {
        private static readonly string HistoryDirectory = "Library/MiniGameToolchain/BuildHistory";

        /// <summary>
        /// 保存一次诊断报告为历史条目。
        /// </summary>
        public static BuildHistoryEntry Save(DiagnosticReport report, string comment = null)
        {
            Directory.CreateDirectory(HistoryDirectory);

            var entry = new BuildHistoryEntry
            {
                Id = DateTime.Now.ToString("yyyyMMdd_HHmmss_") + Guid.NewGuid().ToString("N").Substring(0, 6),
                CreatedAt = DateTime.Now.ToString("O"),
                Comment = comment ?? $"Score {report.OverallScore}",
                OverallScore = report.OverallScore,
                Categories = report.Categories.Values
                    .Select(c => new BuildHistoryCategoryEntry
                    {
                        Name = c.Name,
                        IssueCount = c.IssueCount,
                        TotalPotentialSavingsBytes = c.TotalPotentialSavingsBytes,
                        Score = c.Score
                    })
                    .ToArray(),
                Issues = report.Issues.Select(BuildHistoryEntry.FromDiagnosticIssue).ToArray()
            };

            var path = GetEntryPath(entry.Id);
            var json = JsonUtility.ToJson(entry, true);
            File.WriteAllText(path, json);

            Debug.Log($"[BuildOptimizer] 诊断历史已保存: {entry.Id}");
            return entry;
        }

        /// <summary>
        /// 加载指定 ID 的历史条目。
        /// </summary>
        public static BuildHistoryEntry Load(string id)
        {
            var path = GetEntryPath(id);
            if (!File.Exists(path)) return null;

            var json = File.ReadAllText(path);
            return JsonUtility.FromJson<BuildHistoryEntry>(json);
        }

        /// <summary>
        /// 删除指定历史条目。
        /// </summary>
        public static void Delete(string id)
        {
            var path = GetEntryPath(id);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        /// <summary>
        /// 列出所有历史条目 ID（按时间倒序）。
        /// </summary>
        public static List<string> ListIds()
        {
            if (!Directory.Exists(HistoryDirectory)) return new List<string>();

            return Directory.GetFiles(HistoryDirectory, "history_*.json")
                .Select(Path.GetFileNameWithoutExtension)
                .OrderByDescending(f => f)
                .ToList();
        }

        /// <summary>
        /// 获取历史条目的显示标签。
        /// </summary>
        public static string GetLabel(string id)
        {
            var entry = Load(id);
            if (entry == null) return id;
            var time = entry.CreatedAt;
            if (DateTime.TryParse(time, out var dt))
            {
                time = dt.ToString("yyyy-MM-dd HH:mm");
            }
            return $"{time}  评分:{entry.OverallScore}  {entry.Comment}";
        }

        private static string GetEntryPath(string id)
        {
            return Path.Combine(HistoryDirectory, $"history_{id}.json");
        }
    }
}
