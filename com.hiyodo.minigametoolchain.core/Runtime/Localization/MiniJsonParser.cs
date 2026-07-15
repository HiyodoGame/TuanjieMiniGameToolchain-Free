using System;
using System.Collections.Generic;
using System.Text;

namespace MiniGame.Core.Runtime.Localization
{
    /// <summary>
    /// 极简 JSON 解析器，仅用于本地化数据。
    /// 支持 object / array / string / number / bool / null。
    /// </summary>
    internal static class MiniJsonParser
    {
        public static object Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            var parser = new Parser(json);
            return parser.ParseValue();
        }

        private class Parser
        {
            private readonly string _json;
            private int _index;

            public Parser(string json)
            {
                _json = json;
                _index = 0;
            }

            public object ParseValue()
            {
                SkipWhitespace();
                if (_index >= _json.Length) return null;

                var c = _json[_index];
                switch (c)
                {
                    case '{': return ParseObject();
                    case '[': return ParseArray();
                    case '"': return ParseString();
                    case 't':
                    case 'f': return ParseBool();
                    case 'n': return ParseNull();
                    default:
                        if (c == '-' || char.IsDigit(c)) return ParseNumber();
                        throw new FormatException($"Unexpected character '{c}' at position {_index}");
                }
            }

            private Dictionary<string, object> ParseObject()
            {
                var dict = new Dictionary<string, object>();
                _index++; // {
                SkipWhitespace();

                if (Peek() == '}')
                {
                    _index++;
                    return dict;
                }

                while (true)
                {
                    SkipWhitespace();
                    var key = ParseString();
                    SkipWhitespace();
                    if (Peek() != ':') throw new FormatException("Expected ':' in object");
                    _index++;
                    var value = ParseValue();
                    dict[key] = value;
                    SkipWhitespace();
                    var next = Peek();
                    if (next == ',')
                    {
                        _index++;
                        continue;
                    }

                    if (next == '}')
                    {
                        _index++;
                        return dict;
                    }

                    throw new FormatException("Expected ',' or '}' in object");
                }
            }

            private List<object> ParseArray()
            {
                var list = new List<object>();
                _index++; // [
                SkipWhitespace();

                if (Peek() == ']')
                {
                    _index++;
                    return list;
                }

                while (true)
                {
                    var value = ParseValue();
                    list.Add(value);
                    SkipWhitespace();
                    var next = Peek();
                    if (next == ',')
                    {
                        _index++;
                        continue;
                    }

                    if (next == ']')
                    {
                        _index++;
                        return list;
                    }

                    throw new FormatException("Expected ',' or ']' in array");
                }
            }

            private string ParseString()
            {
                _index++; // opening quote
                var sb = new StringBuilder();
                while (_index < _json.Length)
                {
                    var c = _json[_index++];
                    if (c == '"') return sb.ToString();
                    if (c == '\\' && _index < _json.Length)
                    {
                        var esc = _json[_index++];
                        switch (esc)
                        {
                            case '"': sb.Append('"'); break;
                            case '\\': sb.Append('\\'); break;
                            case '/': sb.Append('/'); break;
                            case 'b': sb.Append('\b'); break;
                            case 'f': sb.Append('\f'); break;
                            case 'n': sb.Append('\n'); break;
                            case 'r': sb.Append('\r'); break;
                            case 't': sb.Append('\t'); break;
                            case 'u':
                                if (_index + 3 < _json.Length)
                                {
                                    var hex = _json.Substring(_index, 4);
                                    sb.Append((char)Convert.ToInt32(hex, 16));
                                    _index += 4;
                                }
                                break;
                            default: sb.Append(esc); break;
                        }
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }

                throw new FormatException("Unterminated string");
            }

            private object ParseNumber()
            {
                var start = _index;
                if (Peek() == '-') _index++;
                while (_index < _json.Length && char.IsDigit(_json[_index])) _index++;
                if (_index < _json.Length && _json[_index] == '.')
                {
                    _index++;
                    while (_index < _json.Length && char.IsDigit(_json[_index])) _index++;
                }

                var number = _json.Substring(start, _index - start);
                if (int.TryParse(number, out var intValue)) return intValue;
                if (double.TryParse(number, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var doubleValue))
                    return doubleValue;
                return number;
            }

            private bool ParseBool()
            {
                if (Match("true"))
                {
                    _index += 4;
                    return true;
                }

                if (Match("false"))
                {
                    _index += 5;
                    return false;
                }

                throw new FormatException("Invalid boolean");
            }

            private object ParseNull()
            {
                if (Match("null"))
                {
                    _index += 4;
                    return null;
                }

                throw new FormatException("Invalid null");
            }

            private void SkipWhitespace()
            {
                while (_index < _json.Length && char.IsWhiteSpace(_json[_index])) _index++;
            }

            private char Peek()
            {
                return _index < _json.Length ? _json[_index] : '\0';
            }

            private bool Match(string text)
            {
                if (_index + text.Length > _json.Length) return false;
                return _json.Substring(_index, text.Length) == text;
            }
        }
    }
}
