using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SMAStudio.Modules.Runbook.Editor.Parser
{
    /// <summary>
    /// Is responsible for parsing Powershell code and mapping the code into
    /// expressions.
    /// </summary>
    public class LanguageParser
    {
        //private string _content = string.Empty;
        //private ExpressionType expr = ExpressionType.None;

        private List<string> _language = new List<string> { "if", "else", "elseif", "for", "foreach", "do", "while", "until", "switch", "break", "continue", "return", "workflow", "inlinescript", "param", "InlineScript", "Workflow", "Param" };
        private List<string> _operators = new List<string> { "-eq", "-gt", "-lt", "-le", "-ge", "-and", "-or", "-ne", "-like", "-notlike", "-match", "-notmatch", "-replace", "-contains", "-notcontains", "-shl", "-shr", "-in", "-notin" };
        private string _content = string.Empty;

        public LanguageParser()
        {
            //_content = content;
            IgnoreBlockMarks = false;
        }

        /// <summary>
        /// Returns a list of different operators that are valid in Powershell
        /// </summary>
        public List<string> Operators
        {
            get { return _operators; }
        }

        /// <summary>
        /// Returns a list of different keywords that is part of the language
        /// </summary>
        public List<string> Language
        {
            get { return _language; }
        }

        /// <summary>
        /// Parses the string and applies our expression types to the information found in the line
        /// </summary>
        /// <param name="content">Content to parse</param>
        /// <returns>List of segments found</returns>
        public List<LanguageSegment> Parse(string content)
        {
            if (String.IsNullOrEmpty(content))
                return new List<LanguageSegment>();

            return InternalParse(content);
        }

        /// <summary>
        /// Parses the string and applies our expression types to the information found in the line
        /// </summary>
        /// <param name="content">Content to parse</param>
        /// <returns>List of segments found</returns>
        private List<LanguageSegment> InternalParse(string content)
        {
            var result = new List<LanguageSegment>();
            var tmpContent = content;

            var expr = ExpressionType.None;
            var contentLength = tmpContent.Length;
            var chunk = new StringBuilder();
            var startPos = 0;
            var lineNumber = 1;
            
            for (int i = 0; i < contentLength; i++)
            {
                if (chunk.Length == 0)
                    startPos = i;

                var ch = tmpContent[i];
                var nextCh = tmpContent.Length > i + 1 ? tmpContent[i + 1] : '\0';

                if (expr == ExpressionType.String || expr == ExpressionType.QuotedString || expr == ExpressionType.SingleQuotedString || expr == ExpressionType.Comment || expr == ExpressionType.MultilineComment)
                {
                    if (ch == '\n')// && _expr != ExpressionType.MultilineComment)
                    {
                        chunk = CreateSegment(expr, chunk, startPos, lineNumber, ref result);
                        lineNumber++;

                        if (expr != ExpressionType.MultilineComment)
                            expr = ExpressionType.None;

                        continue;
                    }
                    else if (ch == '>' && expr == ExpressionType.MultilineComment)
                    {
                        if (tmpContent[i - 1] == '#')
                        {
                            // End the multi line comment
                            chunk.Append(ch);
                            chunk = CreateSegment(expr, chunk, startPos, lineNumber, ref result);
                            expr = ExpressionType.None;
                            continue;
                        }
                    }
                    else if (ch == '"' && expr == ExpressionType.QuotedString)
                    {
                        chunk.Append(ch);
                        chunk = CreateSegment(expr, chunk, startPos, lineNumber, ref result);
                        expr = ExpressionType.None;
                        continue;
                    }
                    else if (ch == '\'' && expr == ExpressionType.SingleQuotedString)
                    {
                        chunk.Append(ch);
                        chunk = CreateSegment(expr, chunk, startPos, lineNumber, ref result);
                        expr = ExpressionType.None;
                        continue;
                    }
                    else if (ch == '$' && expr == ExpressionType.QuotedString)
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
                                else if (tmpContent[a] == ')')
                                    openingBraces--;
                                else
                                    subExpr += tmpContent[a];

                                if (openingBraces == 0)
                                    break;
                            }

                            chunk = CreateSegment(expr, chunk, startPos, lineNumber, ref result);
                            expr = ExpressionType.ExpressionStart;

                            var subResults = InternalParse(subExpr);

                            // We need to increment each sub result start/stop with i to get correct offsets
                            foreach (var subResult in subResults)
                            {
                                subResult.Start += i + 2;
                                subResult.Stop += i + 2;
                            }

                            result.AddRange(subResults);

                            i += subExpr.Length + 1; // we need to increment past the sub expression, since this is already parsed

                            // Add the closing bracket
                            chunk.Append(")");
                            chunk = CreateSegment(expr, chunk, i, lineNumber, ref result);
                            i++;

                            expr = ExpressionType.QuotedString;
                            continue;
                        }
                    }
                    else if (expr == ExpressionType.String && ch == ' ')
                    {
                        // We've reached the end of the string
                        CreateSegment(expr, chunk, startPos, lineNumber, ref result);
                        expr = ExpressionType.None;
                        continue;
                    }
                    else if (expr == ExpressionType.String && ch == '"')
                    {
                        chunk.Append(ch);
                        expr = ExpressionType.QuotedString;
                        continue;
                    }
                    else if (expr == ExpressionType.String && ch == '.')
                    {
                        chunk.Append(ch);
                        expr = ExpressionType.Type;
                    }
                    else if (expr == ExpressionType.String && ch == '\r') // always ignore carrige returns
                        continue;

                    chunk.Append(ch);
                    continue;
                }

                switch (ch)
                {
                    case ',':
                        chunk = CreateSegment(expr, chunk, startPos, lineNumber, ref result);
                        break;
                    case ';':
                    case '\n':
                    case '\t':
                    case ' ':
                        if (ch == '\n')
                        {
                            lineNumber++;
                        }

                        if (chunk.Length == 0)
                            continue;

                        if (_operators.Contains("-" + chunk.ToString()))
                            expr = ExpressionType.Operator;
                        else if (chunk.ToString().Equals("Function", StringComparison.InvariantCultureIgnoreCase))
                            expr = ExpressionType.Function;
                        else if (_language.Contains(chunk.ToString()))
                            expr = ExpressionType.LanguageConstruct;

                        chunk = CreateSegment(expr, chunk, startPos, lineNumber, ref result);

                        if (expr == ExpressionType.Parameter && nextCh != '-')
                            expr = ExpressionType.None;
                        else if ((expr == ExpressionType.Keyword || expr == ExpressionType.LanguageConstruct) && (nextCh == '"' || char.IsLetter(nextCh)))
                        {
                            expr = ExpressionType.String;
                        }
                        else if (expr == ExpressionType.MultilineComment)
                            expr = ExpressionType.MultilineComment;
                        else
                            expr = ExpressionType.None;
                        break;
                    case '-':
                        // Parameter
                        if (expr == ExpressionType.Keyword && chunk.Length > 0)
                        {

                        }
                        else if (chunk.Length == 0 && expr != ExpressionType.QuotedString && expr != ExpressionType.SingleQuotedString && expr != ExpressionType.String)
                        {
                            expr = ExpressionType.Parameter;
                        }
                        else if (expr == ExpressionType.Variable && chunk.Length == 0)
                        {
                            expr = ExpressionType.Operator;
                        }

                        chunk.Append(ch);
                        break;
                    case '\r': // Ignore carrige return
                        break;
                    case '\'':
                        // Start/end of a single quoted string
                        if (expr == ExpressionType.String)
                        {
                            expr = ExpressionType.SingleQuotedString;
                            chunk.Append(ch);
                        }
                        else if (expr == ExpressionType.SingleQuotedString)
                        {
                            // We've reached the end
                            chunk.Append(ch);
                            chunk = CreateSegment(expr, chunk, startPos, lineNumber, ref result);
                            expr = ExpressionType.None;
                        }
                        else
                        {
                            expr = ExpressionType.SingleQuotedString;
                            chunk.Append(ch);
                        }
                        break;
                    case '"':
                        // Start/end of a quoted string
                        if (expr == ExpressionType.String)
                        {
                            expr = ExpressionType.QuotedString;
                            chunk.Append(ch);
                        }
                        else if (expr == ExpressionType.QuotedString)
                        {
                            // We've reached the end
                            chunk.Append(ch);
                            chunk = CreateSegment(expr, chunk, startPos, lineNumber, ref result);
                            expr = ExpressionType.None;
                        }
                        else
                        {
                            expr = ExpressionType.QuotedString;
                            chunk.Append(ch);
                        }
                        break;
                    case '(':
                        // Expression start, eg. (Get-Content -Path "C:\\Test\\Test.txt")
                        // we first need to take care of all data in the chunk before creating expression start
                        if (chunk.Length > 0)
                            chunk = CreateSegment(expr, chunk, startPos, lineNumber, ref result);

                        if (expr == ExpressionType.Property)
                        {
                            expr = ExpressionType.Argument;
                        }

                        if (!IgnoreBlockMarks)
                        {
                            expr = ExpressionType.ExpressionStart;
                            chunk.Append(ch);
                            chunk = CreateSegment(expr, chunk, startPos, lineNumber, ref result);

                            if (expr == ExpressionType.Property)
                                expr = ExpressionType.Argument;
                        }
                        break;
                    case ')':
                        // Expression end, eg. (Get-Content -Path "C:\\Test\\Test.txt")
                        // we first need to take care of all data in the chunk before creating expression end
                        if (chunk.Length > 0)
                            chunk = CreateSegment(expr, chunk, startPos, lineNumber, ref result);

                        if (!IgnoreBlockMarks)
                        {
                            expr = ExpressionType.ExpressionEnd;
                            chunk.Append(ch);
                            chunk = CreateSegment(expr, chunk, startPos, lineNumber, ref result);
                        }

                        expr = ExpressionType.None;
                        break;
                    case '{':
                        // Block start, eg. if (junk) {
                        // we first need to take care of all data in the chunk before creating block start
                        if (chunk.Length > 0)
                            chunk = CreateSegment(expr, chunk, startPos, lineNumber, ref result);

                        if (!IgnoreBlockMarks)
                        {
                            expr = ExpressionType.BlockStart;
                            chunk.Append(ch);
                            chunk = CreateSegment(expr, chunk, startPos, lineNumber, ref result);
                        }
                        break;
                    case '}':
                        // Block end, eg. if (junk) {
                        // we first need to take care of all data in the chunk before creating block end
                        if (chunk.Length > 0)
                            chunk = CreateSegment(expr, chunk, startPos, lineNumber, ref result);

                        if (!IgnoreBlockMarks)
                        {
                            expr = ExpressionType.BlockEnd;
                            chunk.Append(ch);
                            chunk = CreateSegment(expr, chunk, startPos, lineNumber, ref result);
                        }
                        break;
                    case '[':
                        if (chunk.Length > 0)
                            chunk = CreateSegment(expr, chunk, startPos, lineNumber, ref result);

                        if (!IgnoreBlockMarks)
                        {
                            expr = ExpressionType.TypeStart;
                            chunk.Append(ch);
                            chunk = CreateSegment(expr, chunk, startPos, lineNumber, ref result);
                        }

                        expr = ExpressionType.Type;
                        break;
                    case ']':
                        if (chunk.Length > 0)
                            chunk = CreateSegment(expr, chunk, startPos, lineNumber, ref result);

                        if (!IgnoreBlockMarks)
                        {
                            expr = ExpressionType.TypeEnd;
                            chunk.Append(ch);
                            chunk = CreateSegment(expr, chunk, startPos, lineNumber, ref result);
                        }
                        break;
                    case '$':
                        // A variable always starts with a $
                        expr = ExpressionType.Variable;
                        chunk.Append(ch);
                        break;
                    case '=':
                        expr = ExpressionType.Operator;
                        chunk.Append(ch);

                        chunk = CreateSegment(expr, chunk, startPos, lineNumber, ref result);
                        expr = ExpressionType.None;
                        break;
                    case ':':
                        // .NET function call or ($Using:...)
                        // NOTE: It's only a function call if there are two commas after each other, otherwise
                        // its a variable called from an inlinescript etc
                        if (i > 0)
                        {
                            if (tmpContent[i - 1] == ':')
                                expr = ExpressionType.FunctionCall;
                            else if (tmpContent.Length > (i + 1) && tmpContent[i + 1] == ':')
                                expr = ExpressionType.FunctionCall;
                            else
                                expr = ExpressionType.Variable;
                        }

                        chunk.Append(ch);
                        break;
                    case '<':
                        chunk.Append(ch);
                        expr = ExpressionType.MultilineCommentStart;
                        break;
                    case '>':
                        chunk.Append(ch);
                        break;
                    case '#':
                        // Comment
                        if (expr == ExpressionType.MultilineCommentStart)
                            expr = ExpressionType.MultilineComment;
                        else
                            expr = ExpressionType.Comment;

                        chunk.Append(ch);
                        break;
                    case '.':
                        if (expr == ExpressionType.Variable || expr == ExpressionType.Property)
                        {
                            chunk = CreateSegment(expr, chunk, startPos, lineNumber, ref result);
                            expr = ExpressionType.Property;
                        }
                        // TESTING 20160227
                        else if (expr == ExpressionType.String)
                        {
                            expr = ExpressionType.Type;
                        }

                        chunk.Append(ch);
                        break;
                    case '|':
                    case '%':
                    case '+':
                        expr = ExpressionType.Operator;
                        chunk.Append(ch);

                        chunk = CreateSegment(expr, chunk, startPos, lineNumber, ref result);
                        expr = ExpressionType.None;
                        break;
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
                        if (chunk.Length == 0)
                            expr = ExpressionType.Integer;

                        chunk.Append(ch);
                        break;
                    default:
                        if ((expr == ExpressionType.None ||
                                expr == ExpressionType.ExpressionStart ||
                                expr == ExpressionType.ExpressionEnd ||
                                expr == ExpressionType.BlockStart ||
                                expr == ExpressionType.BlockEnd)
                            && char.IsLetter(ch))
                        {
                            if (result.Count > 0
                                && result[result.Count - 1].LineNumber == lineNumber
                                && result[result.Count - 1].Type == ExpressionType.Parameter)
                            {
                                expr = ExpressionType.String;
                            }
                            else
                                expr = ExpressionType.Keyword;
                        }

                        chunk.Append(ch);
                        break;
                }
            }

            if (chunk.Length != 0)
            {
                chunk = CreateSegment(expr, chunk, startPos, lineNumber, ref result);
            }
            
            return result;
        }

        public bool IgnoreBlockMarks { get; set; }

        /// <summary>
        /// Create a contextual segment in the current line
        /// </summary>
        /// <param name="expr">Type of expression in this part of the expression</param>
        /// <param name="chunk">Text that is mapped to the expression</param>
        /// <param name="startPos">Start position in the line</param>
        /// <param name="lineNumber">Line number that we're processing</param>
        /// <param name="segments">Segment list to add the segment to</param>
        /// <returns>An empty chunk</returns>
        private StringBuilder CreateSegment(ExpressionType expr, StringBuilder chunk, int startPos, int lineNumber, ref List<LanguageSegment> segments)
        {
            segments.Add(new LanguageSegment
            {
                Start = startPos,
                Stop = startPos + chunk.Length,
                Type = expr,
                LineNumber = lineNumber,
                Value = chunk.ToString()
            });
            
            chunk.Clear();

            return chunk;
        }
    }
}
