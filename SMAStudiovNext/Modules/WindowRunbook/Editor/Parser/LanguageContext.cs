using SMAStudiovNext.Language;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using SMAStudiovNext.Models;
using SMAStudiovNext.Modules.Runbook.Editor.Completion;

namespace SMAStudio.Modules.Runbook.Editor.Parser
{
    public class LanguageContext
    {
        private object _syncLock = new object();

        /// <summary>
        /// Holds a reference to the currently active expression
        /// </summary>
        private ExpressionType _currentExpression = ExpressionType.None;

        /// <summary>
        /// Determines whether or not we're in a multi line expression or not
        /// </summary>
        private bool _inMultilineExpression = false;

        /// <summary>
        /// Holds the parsed output of our content
        /// </summary>
        private List<LanguageSegment> _segments = null;

        /// <summary>
        /// Language Parser
        /// </summary>
        private readonly LanguageParser _parser;

        private readonly IList<string> _multilineExpressionEndings = new List<string> { "#>", "\"@" };

        private int cachedStartOffset = 0;
        private int cachedStopOffset = 0;
        private List<LanguageSegment> cachedSegments = null;
        //private bool _isInMultilineExpression = false;
        //private ExpressionType _multilineExpressionType = ExpressionType.None;

        public LanguageContext()
        {
            _parser = new LanguageParser();
        }

        public void Parse(string content)
        {
            _segments = _parser.Parse(content);
        }

        public async Task ParseAsync(string content)
        {
            await Task.Run(() => {
                _segments = _parser.Parse(content);
            });
        }

        public ExpressionType PredictContext(int lineNumber, string lineContent)
        {
            if (_segments == null)
                return ExpressionType.None;

            /*if (lineContent.StartsWith("<#"))
            {
                _isInMultilineExpression = true;
                _multilineExpressionType = ExpressionType.MultilineComment;
            }
            else if (lineContent.EndsWith("#>"))
            {
                _isInMultilineExpression = false;
                _multilineExpressionType = ExpressionType.None;
            }*/

            var line = _segments.LastOrDefault(item => item.LineNumber == lineNumber);
            if (line == null)// && !_isInMultilineExpression)
                return ExpressionType.None;
            //else if (line == null && _isInMultilineExpression)
            //    return _multilineExpressionType;

            _currentExpression = line.Type;

            if (_currentExpression == ExpressionType.MultilineComment)
                _inMultilineExpression = true;

            if (_inMultilineExpression)
            {
                if (_multilineExpressionEndings.Contains(lineContent))
                {
                    var expr = _currentExpression;
                    _currentExpression = ExpressionType.None;
                    _inMultilineExpression = false;

                    return expr;
                }

                return _currentExpression;
            }
            
            return ExpressionType.None;
        }
        
        public List<LanguageSegment> GetLine(string content, int startOffset, int endOffset)
        {
            if (_segments == null || content == null)
                return null;

            if (cachedSegments != null && cachedSegments.Count > 0 && cachedStartOffset == startOffset && !content.EndsWith(" "))
            {
                // If we've just added a character that is not a space, we can safely just
                // return our cached information but append one character to the stop offset.
                cachedSegments[cachedSegments.Count - 1].Stop += 1;

                return cachedSegments;
            }
            
            var segments = _parser.Parse(content);

            cachedStartOffset = startOffset;
            cachedStopOffset = endOffset;
            cachedSegments = segments;

            return segments;
        }

        private bool TestRange(int numberToCheck, int bottom, int top)
        {
            return (numberToCheck >= bottom && numberToCheck <= top);
        }

        public LanguageSegment GetCurrentContext(int position)
        {
            return _segments.LastOrDefault(s => s.Start <= position);
        }

        /// <summary>
        /// Retrieve the current context of a position in the document
        /// </summary>
        /// <param name="position">Position to find context of</param>
        public List<LanguageSegment> GetContext(int lineNumber, int position)
        {
            var context = new List<LanguageSegment>();

            List<LanguageSegment> parts = null;
            //lock (_segments)
           // {
            parts = _segments.Where(s => s.LineNumber == lineNumber && s.Start <= position).ToList();
            //}

            parts.Reverse();

            // Remvoe blocks that is opened and closed before we reach our context position
            /*var inBlockToSkip = false;
            var blockedType = ExpressionType.None;
            foreach (var part in parts)
            {
                if (part.Type == ExpressionType.ExpressionEnd || part.Type == ExpressionType.BlockEnd || part.Type == ExpressionType.TypeEnd)
                {
                    inBlockToSkip = true;
                    blockedType = part.Type;
                }
                else if ((part.Type == ExpressionType.ExpressionStart && blockedType == ExpressionType.ExpressionEnd) ||
                    (part.Type == ExpressionType.BlockStart && blockedType == ExpressionType.BlockEnd) ||
                    (part.Type == ExpressionType.TypeStart && blockedType == ExpressionType.TypeEnd))
                {
                    inBlockToSkip = false;
                    continue;
                }

                if (inBlockToSkip)
                    continue;

                if (part.Type == ExpressionType.Type && _parser.IgnoreBlockMarks)
                    continue;

                context.Add(part);
            }
            */
            return parts;
        }

        /// <summary>
        /// Get context name of the position provided
        /// </summary>
        /// <param name="contextualPosition">Position to get context from</param>
        /// <returns>Name of the context</returns>
        public ExpressionType GetContextName(int lineNumber, int contextualPosition)
        {
            lock (_syncLock)
            {
                var context = _segments.Where(s => s.LineNumber == lineNumber && s.Start <= contextualPosition).LastOrDefault();

                //return context[context.Count - 1].Type;
                return (context == null) ? ExpressionType.None : context.Type;
            }
        }

        /// <summary>
        /// Returns a list of variables from the script
        /// </summary>
        /// <returns></returns>
        public List<VariableCompletionData> GetVariables(int position = 0, bool applyUsing = false)
        {
            var list = new List<string>();
            var variables = _segments.Where(s => s.Type == ExpressionType.Variable).Distinct();
            var result = new List<VariableCompletionData>();
            var contextVariables = GetSubContextAssignments(position);
            
            for (int i = 0; i < _segments.Count; i++)
            {
                var variable = _segments[i];

                // We don't want to parse variables that are defined after the cursor position
                if (variable.Start > position)
                    break;

                // We only want variables in this function
                if (variable.Type != ExpressionType.Variable)
                    continue;

                if (!variable.Value.StartsWith("$") || variable.Value.Equals("$"))
                    continue;

                var varName = variable.Value;

                // Check if the variable is found within the same context as we,
                // we don't want to add $Using: to the variable name.
                var inSameContext = contextVariables.FirstOrDefault(item => item.Value.Equals(varName, StringComparison.InvariantCultureIgnoreCase));
                if (inSameContext == null && applyUsing)
                {
                    // We are not in same context, apply $Using: to the variable name
                    if (!varName.StartsWith("$Using:"))
                        varName = varName.Replace("$", "$Using:");
                }
                /*
                if ((variable.Stop < position 
                    || contextVariables.FirstOrDefault(item => item.Value.Equals(varName, StringComparison.InvariantCultureIgnoreCase)) == null)
                    && (!varName.Equals("$true") || !varName.Equals("$false")))
                {
                    if (varName.StartsWith("$Using:") && !applyUsing)
                        varName = varName.Replace("$Using:", "$");
                    else if (applyUsing && !varName.StartsWith("$Using:"))
                        varName = varName.Replace("$", "$Using:");
                }*/

                if (list.Contains(varName))
                    continue;

                variable.Value = varName;

                if (i > 0)
                {
                    var type = _segments[i - 1];
                    if (type.Type == ExpressionType.Type)
                        result.Add(new VariableCompletionData(varName, type.Value));
                    else
                        result.Add(new VariableCompletionData(varName, ""));
                }
                else
                    result.Add(new VariableCompletionData(varName, ""));

                list.Add(varName);
            }

            return result;
        }

        public IList<LanguageSegment> GetSubContextAssignments(int position)
        {
            var result = new List<LanguageSegment>();

            // A sub context is a context where you are required to "call" for variables
            // from the outer scope, eg. InlineScript.
            var segments = _segments.Where(item => item.Start < position).ToList();
            var inlineScriptIndex = segments.FindLastIndex(item => item.Type == ExpressionType.LanguageConstruct && item.Value.Equals("inlinescript", StringComparison.InvariantCultureIgnoreCase));
            var openingBraces = 0;

            if (inlineScriptIndex > -1 && (inlineScriptIndex + 1) < segments.Count)
            {
                for (int i = inlineScriptIndex + 1; i < segments.Count; i++)
                {
                    if (segments[i].Type == ExpressionType.BlockStart)
                        openingBraces++;
                    else if (segments[i].Type == ExpressionType.BlockEnd)
                        openingBraces--;

                    if (openingBraces == 0)
                        break;

                    if (segments[i].Type == ExpressionType.Variable
                            && segments.Count > (i + 1)
                            && segments[i + 1].Type == ExpressionType.Operator
                            && segments[i + 1].Value.Equals("=")) // Assignment
                    {
                        //if (_segments[a].Start > context.Stop)
                        result.Add(segments[i]);
                    }
                }
            }
            
            return result;
        }

        /// <summary>
        /// Try to determine if we're in a sub context (InlineScript) or not
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public bool IsInSubContext(int position)
        {
            // A sub context is a context where you are required to "call" for variables
            // from the outer scope, eg. InlineScript.
            for (int i = 0; i < _segments.Count; i++)
            {
                var context = _segments[i];

                if (context.Type != ExpressionType.LanguageConstruct)
                    continue;

                if (!context.Value.Equals("inlinescript", StringComparison.InvariantCultureIgnoreCase))
                    continue;

                if (_segments.Count > (i + 1))
                {
                    var exprStart = _segments[i + 1];
                    var openingBraces = 0;
                    for (int a = i + 2; a < _segments.Count; a++)
                    {
                        if (_segments[a].Type == ExpressionType.BlockStart)
                        {
                            openingBraces++;
                        }
                        else if (_segments[a].Type == ExpressionType.BlockEnd && openingBraces > 0)
                            openingBraces--;
                        else if (_segments[a].Type == ExpressionType.BlockEnd)
                        {
                            // This is our block match!
                            var exprEnd = _segments[a];

                            if (exprEnd != null)
                            {
                                if (exprStart.Start <= position && position < exprEnd.Stop)
                                {
                                    return true;
                                }
                            }

                            break;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Returns the text of the contextual position
        /// </summary>
        /// <param name="contextualPosition"></param>
        /// <returns></returns>
        public string GetText(int contextualPosition)
        {
            var segment = _segments.Where(s => s.Start <= contextualPosition).LastOrDefault();

            if (segment != null)
                return segment.Value;

            return string.Empty;
        }

        public List<RunbookModelProxy> GetReferences(IList<RunbookModelProxy> runbookList)
        {
            /*var references = _segments.Where(item => 
                item.Type == ExpressionType.Keyword && runbookList.FirstOrDefault(r => 
                    r.RunbookName.Equals(item.Value, StringComparison.InvariantCultureIgnoreCase)) != null)
                .Select(item)
                .ToList();*/
            var references = runbookList.Where(item =>
                _segments.FirstOrDefault(s => 
                    s.Value.Equals(item.RunbookName, StringComparison.InvariantCultureIgnoreCase) 
                    && s.Type == ExpressionType.Keyword) != null)
                .ToList();

            return references;
        }

        /// <summary>
        /// Clears the cached segments
        /// </summary>
        public void ClearCache()
        {
            if (_segments != null)
                _segments.Clear();
        }
    }
}
