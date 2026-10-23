using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MiniGame.Core.Runtime.Localization
{
    /// <summary>
    /// 轻量级多语言管理器。
    /// 支持 CSV/JSON 文本资源加载、运行时语言切换、占位符格式化。
    /// </summary>
    public class LocalizationManager
    {
        public static LocalizationManager Instance { get; } = new LocalizationManager();

        /// <summary>
        /// 当前语言代码，例如 "zh-CN"、"en-US"。
        /// </summary>
        public string CurrentLanguage { get; private set; } = "zh-CN";

        /// <summary>
        /// 语言切换事件。
        /// </summary>
        public event Action LanguageChanged;

        // _entries[language][key] = text
        private readonly Dictionary<string, Dictionary<string, string>> _entries =
            new Dictionary<string, Dictionary<string, string>>();

        /// <summary>
        /// 已加载的所有语言代码。
        /// </summary>
        public IReadOnlyCollection<string> AvailableLanguages => _entries.Keys;

        /// <summary>
        /// 切换当前语言。
        /// </summary>
        public void SetLanguage(string language)
        {
            if (string.IsNullOrEmpty(language)) return;
            if (CurrentLanguage == language) return;

            CurrentLanguage = language;
            LanguageChanged?.Invoke();
        }

        /// <summary>
        /// 清空所有已加载数据。
        /// </summary>
        public void Clear()
        {
            _entries.Clear();
        }

        /// <summary>
        /// 从 TextAsset 加载 CSV 本地化数据。
        /// CSV 格式：第一列 Key，后续列语言代码（如 zh-CN, en-US）。
        /// </summary>
        public void LoadCsv(TextAsset textAsset)
        {
            if (textAsset == null) return;
            LoadCsv(textAsset.text);
        }

        /// <summary>
        /// 从字符串加载 CSV 本地化数据。
        /// </summary>
        public void LoadCsv(string csv)
        {
            if (string.IsNullOrWhiteSpace(csv)) return;

            var lines = SplitLines(csv);
            if (lines.Count == 0) return;

            var headers = ParseCsvLine(lines[0]);
            if (headers.Count < 2) return;

            var languages = headers.GetRange(1, headers.Count - 1);

            for (int i = 1; i < lines.Count; i++)
            {
                var cells = ParseCsvLine(lines[i]);
                if (cells.Count == 0) continue;
                if (string.IsNullOrEmpty(cells[0])) continue;

                var key = cells[0];
                for (int j = 0; j < languages.Count; j++)
                {
                    var lang = languages[j];
                    var text = j + 1 < cells.Count ? cells[j + 1] : string.Empty;
                    SetEntry(lang, key, text);
                }
            }
        }

        /// <summary>
        /// 从 TextAsset 加载 JSON 本地化数据。
        /// 支持格式：
        ///   { "zh-CN": { "key1": "文本1" }, "en-US": { "key1": "Text1" } }
        /// </summary>
        public void LoadJson(TextAsset textAsset)
        {
            if (textAsset == null) return;
            LoadJson(textAsset.text);
        }

        /// <summary>
        /// 从字符串加载 JSON 本地化数据。
        /// </summary>
        public void LoadJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return;

            var root = MiniJsonParser.Parse(json) as Dictionary<string, object>;
            if (root == null) return;

            foreach (var langPair in root)
            {
                var lang = langPair.Key;
                var langDict = langPair.Value as Dictionary<string, object>;
                if (langDict == null) continue;

                foreach (var keyPair in langDict)
                {
                    var text = keyPair.Value?.ToString() ?? string.Empty;
                    SetEntry(lang, keyPair.Key, text);
                }
            }
        }

        /// <summary>
        /// 获取指定 Key 在当前语言下的文本，支持占位符格式化。
        /// </summary>
        public string Get(string key, params object[] args)
        {
            TryGet(key, out var value);
            if (args == null || args.Length == 0) return value;

            try
            {
                return string.Format(CultureInfo.InvariantCulture, value, args);
            }
            catch (FormatException)
            {
                return value;
            }
        }

        /// <summary>
        /// 尝试获取当前语言文本。
        /// </summary>
        public bool TryGet(string key, out string value)
        {
            value = key;
            if (string.IsNullOrEmpty(key)) return false;

            if (_entries.TryGetValue(CurrentLanguage, out var map) && map.TryGetValue(key, out var text))
            {
                value = text;
                return true;
            }

            // 回退：任意已加载语言
            foreach (var pair in _entries)
            {
                if (pair.Value.TryGetValue(key, out var fallback))
                {
                    value = fallback;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 是否包含指定 Key。
        /// </summary>
        public bool HasKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;
            return _entries.Values.Any(map => map.ContainsKey(key));
        }

        private void SetEntry(string language, string key, string text)
        {
            if (string.IsNullOrEmpty(language) || string.IsNullOrEmpty(key)) return;

            if (!_entries.TryGetValue(language, out var map))
            {
                map = new Dictionary<string, string>();
                _entries[language] = map;
            }

            map[key] = text;
        }

        private static List<string> SplitLines(string text)
        {
            var lines = new List<string>();
            var sb = new StringBuilder();
            for (int i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (c == '\r')
                {
                    if (i + 1 < text.Length && text[i + 1] == '\n') i++;
                    lines.Add(sb.ToString());
                    sb.Clear();
                }
                else if (c == '\n')
                {
                    lines.Add(sb.ToString());
                    sb.Clear();
                }
                else
                {
                    sb.Append(c);
                }
            }

            if (sb.Length > 0) lines.Add(sb.ToString());
            return lines;
        }

        private static List<string> ParseCsvLine(string line)
        {
            var cells = new List<string>();
            var sb = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                var c = line[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            sb.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                else
                {
                    if (c == '"')
                    {
                        inQuotes = true;
                    }
                    else if (c == ',')
                    {
                        cells.Add(sb.ToString());
                        sb.Clear();
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
            }

            cells.Add(sb.ToString());
            return cells;
        }
    }
}
