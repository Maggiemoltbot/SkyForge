// Adapted from Unity's MiniJSON (MIT License)
// Source: https://gist.github.com/darktable/1411710
// Provides lightweight JSON serialization/deserialization without external dependencies.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace SkyForge.MCP
{
    internal static class MiniJson
    {
        public static object Deserialize(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            return Parser.Parse(json);
        }

        public static string Serialize(object obj)
        {
            return Serializer.Serialize(obj);
        }

        private sealed class Parser : IDisposable
        {
            private const string WordBreak = " \t\r\n{}[],:\"";

            private enum Token
            {
                None,
                CurlyOpen,
                CurlyClose,
                SquaredOpen,
                SquaredClose,
                Colon,
                Comma,
                String,
                Number,
                True,
                False,
                Null
            }

            private readonly string _json;
            private int _index;

            private Parser(string json)
            {
                _json = json;
            }

            public static object Parse(string json)
            {
                using (var instance = new Parser(json))
                {
                    return instance.ParseValue();
                }
            }

            public void Dispose()
            {
                // Nothing to dispose, exists for using pattern symmetry.
            }

            private Dictionary<string, object> ParseObject()
            {
                var table = new Dictionary<string, object>(StringComparer.Ordinal);

                // skip '{'
                _index++;

                while (true)
                {
                    EatWhitespace();
                    if (IsEndOfInput)
                    {
                        return null;
                    }

                    Token token = LookAhead;
                    if (token == Token.Comma)
                    {
                        _index++;
                        continue;
                    }

                    if (token == Token.CurlyClose)
                    {
                        _index++;
                        return table;
                    }

                    string name = ParseString();
                    if (name == null)
                    {
                        return null;
                    }

                    Token nextToken = NextToken();
                    if (nextToken != Token.Colon)
                    {
                        return null;
                    }

                    object value = ParseValue();
                    table[name] = value;
                }
            }

            private List<object> ParseArray()
            {
                var array = new List<object>();

                // skip '['
                _index++;

                bool parsing = true;
                while (parsing)
                {
                    EatWhitespace();
                    if (IsEndOfInput)
                    {
                        return null;
                    }

                    Token token = LookAhead;
                    switch (token)
                    {
                        case Token.Comma:
                            _index++;
                            break;
                        case Token.SquaredClose:
                            _index++;
                            parsing = false;
                            break;
                        default:
                            object value = ParseValue();
                            array.Add(value);
                            break;
                    }
                }

                return array;
            }

            private object ParseValue()
            {
                EatWhitespace();
                if (IsEndOfInput)
                {
                    return null;
                }

                switch (LookAhead)
                {
                    case Token.String:
                        return ParseString();
                    case Token.Number:
                        return ParseNumber();
                    case Token.CurlyOpen:
                        return ParseObject();
                    case Token.SquaredOpen:
                        return ParseArray();
                    case Token.True:
                        _index += 4;
                        return true;
                    case Token.False:
                        _index += 5;
                        return false;
                    case Token.Null:
                        _index += 4;
                        return null;
                    default:
                        return null;
                }
            }

            private string ParseString()
            {
                if (PeekChar != '\"')
                {
                    return null;
                }

                var builder = new StringBuilder();
                _index++; // Skip opening quote

                bool parsing = true;
                while (parsing)
                {
                    if (IsEndOfInput)
                    {
                        break;
                    }

                    char c = NextChar;
                    switch (c)
                    {
                        case '\"':
                            parsing = false;
                            break;
                        case '\\':
                            if (IsEndOfInput)
                            {
                                parsing = false;
                                break;
                            }

                            c = NextChar;
                            switch (c)
                            {
                                case '"':
                                case '\\':
                                case '/':
                                    builder.Append(c);
                                    break;
                                case 'b':
                                    builder.Append('\b');
                                    break;
                                case 'f':
                                    builder.Append('\f');
                                    break;
                                case 'n':
                                    builder.Append('\n');
                                    break;
                                case 'r':
                                    builder.Append('\r');
                                    break;
                                case 't':
                                    builder.Append('\t');
                                    break;
                                case 'u':
                                    if (_index + 4 <= _json.Length)
                                    {
                                        string hex = _json.Substring(_index, 4);
                                        if (uint.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var codePoint))
                                        {
                                            builder.Append((char)codePoint);
                                            _index += 4;
                                        }
                                    }
                                    break;
                            }
                            break;
                        default:
                            builder.Append(c);
                            break;
                    }
                }

                return builder.ToString();
            }

            private object ParseNumber()
            {
                int lastIndex = GetLastIndexOfNumber(_index);
                int charLength = (lastIndex - _index) + 1;
                string numberString = _json.Substring(_index, charLength);
                _index = lastIndex + 1;

                if (numberString.IndexOf('.', StringComparison.Ordinal) != -1 ||
                    numberString.IndexOf('e', StringComparison.OrdinalIgnoreCase) != -1)
                {
                    if (double.TryParse(numberString, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double doubleResult))
                    {
                        return doubleResult;
                    }
                }
                else
                {
                    if (long.TryParse(numberString, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out long longResult))
                    {
                        if (longResult >= int.MinValue && longResult <= int.MaxValue)
                        {
                            return (int)longResult;
                        }

                        return longResult;
                    }
                }

                return 0;
            }

            private void EatWhitespace()
            {
                while (!IsEndOfInput)
                {
                    char c = PeekChar;
                    if (char.IsWhiteSpace(c))
                    {
                        _index++;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            private char PeekChar => _index < _json.Length ? _json[_index] : '\0';

            private char NextChar => _index < _json.Length ? _json[_index++] : '\0';

            private bool IsEndOfInput => _index >= _json.Length;

            private int GetLastIndexOfNumber(int index)
            {
                int lastIndex = index;
                while (lastIndex < _json.Length)
                {
                    if (WordBreak.IndexOf(_json[lastIndex]) != -1)
                    {
                        break;
                    }
                    lastIndex++;
                }

                return lastIndex - 1;
            }

            private Token LookAhead
            {
                get
                {
                    int saveIndex = _index;
                    return NextTokenCore(ref saveIndex);
                }
            }

            private Token NextToken()
            {
                int saveIndex = _index;
                Token token = NextTokenCore(ref saveIndex);
                _index = saveIndex;
                return token;
            }

            private Token NextTokenCore(ref int index)
            {
                while (index < _json.Length && char.IsWhiteSpace(_json[index]))
                {
                    index++;
                }

                if (index == _json.Length)
                {
                    return Token.None;
                }

                char c = _json[index];
                index++;

                switch (c)
                {
                    case '{':
                        return Token.CurlyOpen;
                    case '}':
                        return Token.CurlyClose;
                    case '[':
                        return Token.SquaredOpen;
                    case ']':
                        return Token.SquaredClose;
                    case ',':
                        return Token.Comma;
                    case ':':
                        return Token.Colon;
                    case '"':
                        return Token.String;
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                    case '-':
                        return Token.Number;
                }

                index--;
                int remainingLength = _json.Length - index;

                if (remainingLength >= 4 && string.Compare(_json, index, "true", 0, 4, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    index += 4;
                    return Token.True;
                }

                if (remainingLength >= 5 && string.Compare(_json, index, "false", 0, 5, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    index += 5;
                    return Token.False;
                }

                if (remainingLength >= 4 && string.Compare(_json, index, "null", 0, 4, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    index += 4;
                    return Token.Null;
                }

                return Token.None;
            }
        }

        private sealed class Serializer
        {
            private readonly StringBuilder _builder = new StringBuilder();

            private Serializer()
            {
            }

            public static string Serialize(object obj)
            {
                var serializer = new Serializer();
                serializer.SerializeValue(obj);
                return serializer._builder.ToString();
            }

            private void SerializeValue(object value)
            {
                if (value == null)
                {
                    _builder.Append("null");
                    return;
                }

                if (value is string s)
                {
                    SerializeString(s);
                }
                else if (value is bool b)
                {
                    _builder.Append(b ? "true" : "false");
                }
                else if (IsNumeric(value))
                {
                    SerializeNumber(value);
                }
                else if (value is IDictionary dictionary)
                {
                    SerializeObject(dictionary);
                }
                else if (value is IList list)
                {
                    SerializeArray(list);
                }
                else if (value is char ch)
                {
                    SerializeString(new string(ch, 1));
                }
                else
                {
                    SerializeString(value.ToString());
                }
            }

            private void SerializeObject(IDictionary obj)
            {
                bool first = true;
                _builder.Append('{');
                foreach (DictionaryEntry entry in obj)
                {
                    if (!first)
                    {
                        _builder.Append(',');
                    }
                    first = false;

                    SerializeString(entry.Key.ToString());
                    _builder.Append(':');
                    SerializeValue(entry.Value);
                }
                _builder.Append('}');
            }

            private void SerializeArray(IList array)
            {
                _builder.Append('[');
                bool first = true;
                foreach (var element in array)
                {
                    if (!first)
                    {
                        _builder.Append(',');
                    }
                    first = false;
                    SerializeValue(element);
                }
                _builder.Append(']');
            }

            private void SerializeString(string str)
            {
                _builder.Append('"');
                foreach (char c in str)
                {
                    switch (c)
                    {
                        case '\\':
                            _builder.Append("\\\\");
                            break;
                        case '"':
                            _builder.Append("\\\"");
                            break;
                        case '\n':
                            _builder.Append("\\n");
                            break;
                        case '\r':
                            _builder.Append("\\r");
                            break;
                        case '\t':
                            _builder.Append("\\t");
                            break;
                        case '\b':
                            _builder.Append("\\b");
                            break;
                        case '\f':
                            _builder.Append("\\f");
                            break;
                        default:
                            if (c < ' ')
                            {
                                _builder.AppendFormat("\\u{0:X4}", (int)c);
                            }
                            else
                            {
                                _builder.Append(c);
                            }
                            break;
                    }
                }
                _builder.Append('"');
            }

            private void SerializeNumber(object number)
            {
                switch (number)
                {
                    case int i:
                        _builder.Append(i.ToString(System.Globalization.CultureInfo.InvariantCulture));
                        break;
                    case long l:
                        _builder.Append(l.ToString(System.Globalization.CultureInfo.InvariantCulture));
                        break;
                    case float f:
                        _builder.Append(f.ToString(System.Globalization.CultureInfo.InvariantCulture));
                        break;
                    case double d:
                        _builder.Append(d.ToString(System.Globalization.CultureInfo.InvariantCulture));
                        break;
                    case decimal dec:
                        _builder.Append(dec.ToString(System.Globalization.CultureInfo.InvariantCulture));
                        break;
                    default:
                        _builder.Append(Convert.ToDouble(number, System.Globalization.CultureInfo.InvariantCulture)
                            .ToString(System.Globalization.CultureInfo.InvariantCulture));
                        break;
                }
            }

            private static bool IsNumeric(object value)
            {
                return value is sbyte || value is byte || value is short || value is ushort ||
                       value is int || value is uint || value is long || value is ulong ||
                       value is float || value is double || value is decimal;
            }
        }
    }
}
