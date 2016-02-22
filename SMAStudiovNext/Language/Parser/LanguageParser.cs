using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SMAStudiovNext.Language
{
    public class LanguageParser
    {
        //private string _content = string.Empty;
        private ExpressionType _expr = ExpressionType.None;

        private List<LanguageSegment> _segments = new List<LanguageSegment>();
        private List<string> _language = new List<string> { "if", "else", "elseif", "for", "foreach", "do", "while", "until", "switch", "break", "continue", "return" };
        private List<string> _operators = new List<string> { "-eq", "-gt", "-lt", "-le", "-ge", "-and", "-or", "-ne", "-like", "-notlike", "-match", "-notmatch", "-replace", "-contains", "-notcontains", "-shl", "-shr", "-in", "-notin" };
        private string _content = string.Empty;

        public LanguageParser()
        {
            //_content = content;
            IgnoreBlockMarks = false;
        }

        public List<string> Operators
        {
            get { return _operators; }
        }

        public List<string> Language
        {
            get { return _language; }
        }

        public void Clear()
        {
            _segments.Clear();
        }

        public List<LanguageSegment> Parse(string content)
        {
            if (_content.Equals(content) && _segments.Count > 0)
                return _segments;

            _content = content;
            Clear();

            InternalParse(_content);

            return _segments;
        }

        private List<LanguageSegment> InternalParse(string content)
        {
            var tmpContent = content + "\n";

            var contentLength = tmpContent.Length;
            var chunk = new StringBuilder();
            var startPos = 0;

            for (int i = 0; i < contentLength; i++)
            {
                if (chunk.Length == 0)
                    startPos = i;

                var ch = tmpContent[i];
                var nextCh = tmpContent.Length > i + 1 ? tmpContent[i + 1] : '\0';

                if (_expr == ExpressionType.String || _expr == ExpressionType.QuotedString || _expr == ExpressionType.Comment || _expr == ExpressionType.MultilineComment)
                {
                    if (ch == '\n' && _expr != ExpressionType.MultilineComment)
                    {
                        chunk = CreateSegment(chunk, startPos, i);

                        if (_expr != ExpressionType.MultilineComment)
                            _expr = ExpressionType.None;

                        continue;
                    }
                    else if (ch == '>' && _expr == ExpressionType.MultilineComment)
                    {
                        if (tmpContent[i - 1] == '#')
                        {
                            // End the multi line comment
                            chunk = CreateSegment(chunk, startPos, i);
                            _expr = ExpressionType.None;
                            continue;
                        }
                    }
                    else if (ch == '"' && _expr == ExpressionType.QuotedString)
                    {
                        chunk = CreateSegment(chunk, startPos, i);
                        _expr = ExpressionType.None;
                        continue;
                    }
                    else if (ch == '$' && _expr == ExpressionType.QuotedString)
                    {
                        if (nextCh == '(' && (i + 2) < tmpContent.Length)
                        {
                            // We have found a part of the string which evaluates a PS expression,
                            // we need to take this into account
                            int openingBraces = 0;
                            string subExpr = string.Empty;

                            for (int a = i + 1; a < tmpContent.Length; a++)
                            {
                                if (tmpContent[a] == '(')
                                    openingBraces++;

                                subExpr += tmpContent[a];

                                if (tmpContent[a] == ')')
                                    openingBraces--;

                                if (openingBraces == 0)
                                    break;
                            }

                            CreateSegment(chunk, startPos, i);
                            _expr = ExpressionType.ExpressionStart;

                            InternalParse(subExpr);
                            i += subExpr.Length; // we need to increment past the sub expression, since this is already parsed

                            _expr = ExpressionType.QuotedString;
                            continue;
                            //var result = Parse(subExpr);
                            //_segments.AddRange(result);
                        }
                    }
                    else if (_expr == ExpressionType.String && ch == ' ')
                    {
                        // We've reached the end of the string
                        CreateSegment(chunk, startPos, i);
                        _expr = ExpressionType.None;
                        continue;
                    }
                    else if (_expr == ExpressionType.String && ch == '"')
                    {
                        _expr = ExpressionType.QuotedString;
                        continue;
                    }

                    chunk.Append(ch);
                    continue;
                }

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

                        if (_operators.Contains("-" + chunk.ToString()))
                            _expr = ExpressionType.Operator;
                        else if (chunk.ToString().Equals("Function", StringComparison.InvariantCultureIgnoreCase))
                            _expr = ExpressionType.Function;
                        else if (_language.Contains(chunk.ToString()))
                            _expr = ExpressionType.LanguageConstruct;

                        chunk = CreateSegment(chunk, startPos, i);

                        if (_expr == ExpressionType.Parameter && nextCh != '-')
                            _expr = ExpressionType.None;
                        else if (_expr == ExpressionType.Keyword && (nextCh == '"' || char.IsLetter(nextCh)))
                        {
                            //if (nextCh == '"')
                            //    _expr = ExpressionType.QuotedString;
                            //else
                                _expr = ExpressionType.String;
                        }
                        else if (_expr == ExpressionType.MultilineComment)
                            _expr = ExpressionType.MultilineComment;
                        else
                            _expr = ExpressionType.None;
                        break;
                    case '-':
                        // Parameter
                        if (_expr == ExpressionType.Keyword && chunk.Length > 0)
                        {

                        }
                        else if (chunk.Length == 0 && _expr != ExpressionType.QuotedString && _expr != ExpressionType.String)
                        {
                            _expr = ExpressionType.Parameter;
                        }
                        else if (_expr == ExpressionType.Variable && chunk.Length == 0)
                        {
                            _expr = ExpressionType.Operator;
                        }

                        chunk.Append(ch);
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
                        // we first need to take care of all data in the chunk before creating expression start
                        if (chunk.Length > 0)
                            chunk = CreateSegment(chunk, startPos, i);

                        if (_expr == ExpressionType.Property)
                        {
                            _expr = ExpressionType.Argument;
                        }

                        if (!IgnoreBlockMarks)
                        {
                            _expr = ExpressionType.ExpressionStart;
                            chunk.Append(ch);
                            chunk = CreateSegment(chunk, startPos, i);

                            if (_expr == ExpressionType.Property)
                                _expr = ExpressionType.Argument;
                        }
                        break;
                    case ')':
                        // Expression end, eg. (Get-Content -Path "C:\\Test\\Test.txt")
                        // we first need to take care of all data in the chunk before creating expression end
                        if (chunk.Length > 0)
                            chunk = CreateSegment(chunk, startPos, i);

                        if (!IgnoreBlockMarks)
                        {
                            _expr = ExpressionType.ExpressionEnd;
                            chunk.Append(ch);
                            chunk = CreateSegment(chunk, startPos, i);
                        }

                        _expr = ExpressionType.None;
                        break;
                    case '{':
                        // Block start, eg. if (junk) {
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
                        // .NET function call or ($Using:...)
                        // NOTE: It's only a function call if there are two commas after each other, otherwise
                        // its a variable called from an inlinescript etc
                        if (i > 0)
                        {
                            if (tmpContent[i - 1] == ':')
                                _expr = ExpressionType.FunctionCall;
                            else if (tmpContent.Length > i && tmpContent[i + 1] == ':')
                                _expr = ExpressionType.FunctionCall;
                            else
                                _expr = ExpressionType.Variable;
                        }

                        chunk.Append(ch);
                        break;
                    case '<':
                        _expr = ExpressionType.MultilineCommentStart;
                        break;
                    case '>':
                        if (_expr == ExpressionType.MultilineComment)
                        {
                            chunk = CreateSegment(chunk, startPos, i);
                            _expr = ExpressionType.None;
                        }
                        break;
                    case '#':
                        // Comment
                        if (_expr == ExpressionType.MultilineCommentStart)
                            _expr = ExpressionType.MultilineComment;
                        else
                            _expr = ExpressionType.Comment;
                        break;
                    case '.':
                        if (_expr == ExpressionType.Variable || _expr == ExpressionType.Property)
                        {
                            chunk = CreateSegment(chunk, startPos, i);
                            _expr = ExpressionType.Property;
                        }
                        break;
                    case '|':
                    case '%':
                    case '+':
                        _expr = ExpressionType.Operator;
                        chunk.Append(ch);

                        chunk = CreateSegment(chunk, startPos, i);
                        _expr = ExpressionType.None;
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

            if (chunk.Length != 0)
            {
                chunk = CreateSegment(chunk, startPos, contentLength);
            }
            
            return _segments;
        }

        public bool IgnoreBlockMarks { get; set; }

        private StringBuilder CreateSegment(StringBuilder chunk, int startPos, int endPos)
        {
            _segments.Add(new LanguageSegment
            {
                Start = startPos,
                Stop = startPos + chunk.Length,
                Type = _expr,
                Value = chunk.ToString()
            });
            
            chunk.Clear();

            return chunk;
        }
    }
}
