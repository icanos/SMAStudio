using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudio.Language
{
    public class PowershellParser
    {
        //private string _content = string.Empty;
        private ExpressionType _expr = ExpressionType.None;

        private List<PowershellSegment> _segments = new List<PowershellSegment>();
        private List<string> _operators = new List<string> { "-eq", "-gt", "-lt", "-le", "-ge", "-and", "-or", "-ne", "-like", "-notlike", "-match", "-notmatch", "-replace", "-contains", "-notcontains", "-shl", "-shr", "-in", "-notin" };

        public PowershellParser()
        {
            //_content = content;
        }

        public void Clear()
        {
            _segments.Clear();
        }

        public List<PowershellSegment> Parse(string _content)
        {
            var tmpContent = _content + "\n";

            var contentLength = tmpContent.Length;
            var chunk = new StringBuilder();
            var startPos = 0;

            for (int i = 0; i < contentLength; i++)
            {
                if (chunk.Length == 0)
                    startPos = i;

                var ch = tmpContent[i];
                var nextCh = tmpContent.Length > i + 1 ? tmpContent[i + 1] : '\0';

                switch (ch)
                {
                    case ',':
                        chunk = CreateSegment(chunk, startPos, i);
                        break;
                    case ';':
                    case '\n':
                    case '\t':
                    case ' ':
                        if (chunk.Length == 0)
                            continue;

                        if (ch == ' ' && _expr == ExpressionType.QuotedString)
                        {
                            chunk.Append(ch);
                            continue;
                        }

                        if (_operators.Contains("-" + chunk.ToString()))
                            _expr = ExpressionType.Operator;
                        else if (chunk.ToString().Equals("Function", StringComparison.InvariantCultureIgnoreCase))
                            _expr = ExpressionType.Function;
                        /*else if (chunk.ToString().Equals("Import-Module", StringComparison.InvariantCultureIgnoreCase) ||
                            chunk.ToString().Equals("ipmo", StringComparison.InvariantCultureIgnoreCase))
                            _expr = ExpressionType.ImportModule;*/

                        chunk = CreateSegment(chunk, startPos, i);

                        if (_expr == ExpressionType.Parameter && nextCh != '-')
                            _expr = ExpressionType.String;
                        else if (_expr == ExpressionType.Keyword && (nextCh == '"' || char.IsLetter(nextCh)))
                            _expr = ExpressionType.String;
                        else
                            _expr = ExpressionType.None;
                        break;
                    case '-':
                        // Parameter
                        if (_expr == ExpressionType.Keyword && chunk.Length > 0)
                        {
                            chunk.Append(ch);
                        }
                        else if (chunk.Length == 0 && _expr != ExpressionType.QuotedString && _expr != ExpressionType.String)
                        //else if (_expr == ExpressionType.Keyword)
                        {
                            _expr = ExpressionType.Parameter;
                        }
                        else if (_expr == ExpressionType.Variable && chunk.Length == 0)
                        {
                            _expr = ExpressionType.Operator;
                        }

                        break;
                    case '\r':
                        break;
                    case '"':
                        // Start/end of a quoted string
                        if (_expr == ExpressionType.String)
                            _expr = ExpressionType.QuotedString;
                        else if (_expr == ExpressionType.QuotedString)
                        {
                            // We've reached the end
                            chunk = CreateSegment(chunk, startPos, i);
                            _expr = ExpressionType.None;
                        }
                        else
                        {
                            _expr = ExpressionType.QuotedString;
                        }
                        break;
                    case '(':
                        // Expression start, eg. (Get-Content -Path "C:\\Test\\Test.txt")
                        if (_expr == ExpressionType.QuotedString)
                        {
                            chunk.Append(ch);
                            continue;
                        }

                        // we first need to take care of all data in the chunk before creating expression start
                        if (chunk.Length > 0)
                            chunk = CreateSegment(chunk, startPos, i);

                        if (!IgnoreBlockMarks)
                        {
                            _expr = ExpressionType.ExpressionStart;
                            chunk.Append(ch);
                            chunk = CreateSegment(chunk, startPos, i);
                        }
                        break;
                    case ')':
                        // Expression end, eg. (Get-Content -Path "C:\\Test\\Test.txt")
                        if (_expr == ExpressionType.QuotedString)
                        {
                            chunk.Append(ch);
                            continue;
                        }

                        // we first need to take care of all data in the chunk before creating expression end
                        if (chunk.Length > 0)
                            chunk = CreateSegment(chunk, startPos, i);

                        if (!IgnoreBlockMarks)
                        {
                            _expr = ExpressionType.ExpressionEnd;
                            chunk.Append(ch);
                            chunk = CreateSegment(chunk, startPos, i);
                        }
                        break;
                    case '{':
                        // Block start, eg. if (junk) {
                        if (_expr == ExpressionType.QuotedString)
                        {
                            chunk.Append(ch);
                            continue;
                        }

                        // we first need to take care of all data in the chunk before creating block start
                        if (chunk.Length > 0)
                            chunk = CreateSegment(chunk, startPos, i);

                        if (!IgnoreBlockMarks)
                        {
                            _expr = ExpressionType.BlockStart;
                            chunk.Append(ch);
                            chunk = CreateSegment(chunk, startPos, i);
                        }
                        break;
                    case '}':
                        // Block end, eg. if (junk) {
                        if (_expr == ExpressionType.QuotedString)
                        {
                            chunk.Append(ch);
                            continue;
                        }

                        // we first need to take care of all data in the chunk before creating block end
                        if (chunk.Length > 0)
                            chunk = CreateSegment(chunk, startPos, i);

                        if (!IgnoreBlockMarks)
                        {
                            _expr = ExpressionType.BlockEnd;
                            chunk.Append(ch);
                            chunk = CreateSegment(chunk, startPos, i);
                        }
                        break;
                    case '[':
                        if (_expr == ExpressionType.QuotedString)
                        {
                            chunk.Append(ch);
                            continue;
                        }

                        if (chunk.Length > 0)
                            chunk = CreateSegment(chunk, startPos, i);

                        if (!IgnoreBlockMarks)
                        {
                            _expr = ExpressionType.TypeStart;
                            chunk.Append(ch);
                            chunk = CreateSegment(chunk, startPos, i);
                        }

                        _expr = ExpressionType.Type;
                        break;
                    case ']':
                        if (_expr == ExpressionType.QuotedString)
                        {
                            chunk.Append(ch);
                            continue;
                        }

                        if (chunk.Length > 0)
                            chunk = CreateSegment(chunk, startPos, i);

                        if (!IgnoreBlockMarks)
                        {
                            _expr = ExpressionType.TypeEnd;
                            chunk.Append(ch);
                            chunk = CreateSegment(chunk, startPos, i);
                        }
                        break;
                    case '$':
                        // A variable always starts with a $
                        _expr = ExpressionType.Variable;
                        chunk.Append(ch);
                        break;
                    case '=':
                        _expr = ExpressionType.Operator;
                        chunk.Append(ch);

                        chunk = CreateSegment(chunk, startPos, i);
                        _expr = ExpressionType.None;
                        break;
                    case ':':
                        // .NET function call
                        _expr = ExpressionType.FunctionCall;
                        break;
                    default:
                        if ((_expr == ExpressionType.None ||
                                _expr == ExpressionType.ExpressionStart ||
                                _expr == ExpressionType.ExpressionEnd ||
                                _expr == ExpressionType.BlockStart ||
                                _expr == ExpressionType.BlockEnd)
                            && char.IsLetter(ch))
                        {
                            _expr = ExpressionType.Keyword;
                        }

                        chunk.Append(ch);
                        break;
                }
            }

            return _segments;
        }

        public bool IgnoreBlockMarks { get; set; }

        private StringBuilder CreateSegment(StringBuilder chunk, int startPos, int endPos)
        {
            _segments.Add(new PowershellSegment
            {
                Start = startPos,
                Stop = endPos,
                Type = _expr,
                Value = chunk.ToString()
            });

            chunk.Clear();

            return chunk;
        }
    }
}
