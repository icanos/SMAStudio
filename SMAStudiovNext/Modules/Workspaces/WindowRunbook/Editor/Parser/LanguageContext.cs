using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Language;
using System.Threading.Tasks;
using SMAStudiovNext.Models;
using SMAStudiovNext.Modules.WindowRunbook.Editor.Completion;
using Microsoft.Windows.PowerShell.ScriptAnalyzer;
using SMAStudiovNext.Services;
using System.Management.Automation;
using Microsoft.Windows.PowerShell.ScriptAnalyzer.Generic;
using Caliburn.Micro;

namespace SMAStudiovNext.Modules.WindowRunbook.Editor.Parser
{
    public delegate void OnParseErrorDelegate(object sender, ParseErrorEventArgs e);
    public delegate void OnClearParseErrorsDelegate(object sender, EventArgs e);

    public class LanguageContext
    {
        private readonly object _lock = new object();

        private Token[] _tokens;
        private ParseError[] _parseErrors;
        private ScriptBlockAst _scriptBlock;
        private bool _currentlyHasParseErrors;

        public LanguageContext()
        {
            
        }

        public event OnParseErrorDelegate OnParseError;
        public event OnClearParseErrorsDelegate OnClearParseErrors;
        public event EventHandler<AnalysisEventArgs> OnAnalysisCompleted;

        /// <summary>
        /// Parse the runbook content
        /// </summary>
        /// <param name="content"></param>
        public void Parse(string content)
        {
            if (_tokens == null || _scriptBlock == null)
                IsDirty = true;

            if (!IsDirty)
                return;

            _scriptBlock = System.Management.Automation.Language.Parser.ParseInput(content, out _tokens, out _parseErrors);

            if (_parseErrors != null && _parseErrors.Length > 0)
            {
                _currentlyHasParseErrors = true;
                OnParseError?.Invoke(this, new ParseErrorEventArgs(_parseErrors));
            }
            else if (_currentlyHasParseErrors)
            {
                _currentlyHasParseErrors = false;
                OnClearParseErrors?.Invoke(this, new EventArgs());
            }

            // Analyze the code too
            if (SettingsService.CurrentSettings.EnableCodeAnalysis && !string.IsNullOrEmpty(content) && IsDirty)
            {
                Task.Run(() =>
                {
                    var records = AnalyzerService.Analyze(_scriptBlock, _tokens);//ScriptAnalyzer.Instance.AnalyzeSyntaxTree(_scriptBlock, _tokens, string.Empty);

                    if (SettingsService.CurrentSettings.EnableCodeAnalysis)
                    {// && (_cachedRecords == null || _cachedRecords.Count() != records.Count()))
                        Execute.OnUIThread(() => { OnAnalysisCompleted?.Invoke(this, new AnalysisEventArgs(records)); });
                    }
                });
            }
        }

        /// <summary>
        /// Get a list of completion data objects of variables
        /// </summary>
        /// <param name="position">Position to get variables up until</param>
        /// <param name="applyUsing">If $Using: should be applied to variables</param>
        /// <returns>List of completion data</returns>
        public List<VariableCompletionData> GetVariables(int position, bool applyUsing = false)
        {
            var variables = new List<VariableCompletionData>();
            var takenVariables = new List<string>();
            var segments = default(IList<Token>);

            lock (_lock)
                segments = _tokens.Where(item => item.Extent.StartOffset <= position && item.Kind == TokenKind.Variable).ToList();
            //segments = GetAssigningVariables(position);

            for (var i = 0; i < segments.Count; i++)
            {
                var token = segments[i];

                var variableName = ApplyUsingStatement(token, position, applyUsing);

                if (takenVariables.Contains(variableName))
                    continue;

                var segmentBefore = default(Token);

                if ((i - 1) > 0)
                    segmentBefore = segments[i - 1];

#if PSV3
                if (segmentBefore != null && segmentBefore.TokenFlags == TokenFlags.TypeName)
#else
                if (segmentBefore != null && segmentBefore.Kind == TokenKind.Type)
#endif
                    variables.Add(new VariableCompletionData(variableName, segmentBefore.Extent.Text));
                else
                    variables.Add(new VariableCompletionData(variableName, ""));

                takenVariables.Add(variableName);
            }

            return variables;
        }

        /// <summary>
        /// Determine if we're in a inlinescript or not
        /// </summary>
        /// <param name="position">Position to check</param>
        /// <returns>True or false</returns>
        public bool IsInSubContext(int position)
        {
            var tokenBlock = GetSubContext(position);

            if (position >= tokenBlock?.StartOffset && position <= tokenBlock.EndOffset)
                return true;

            return false;
        }

        /// <summary>
        /// Get the start end end offset of the InlineScript
        /// </summary>
        /// <param name="position">Position to check</param>
        /// <returns>Start/end offset of the block</returns>
        public TokenBlock GetSubContext(int position)
        {
            var segments = default(List<Token>);

            lock (_lock)
                segments = _tokens.Where(item => item.Extent.StartOffset <= position).ToList();

            if (segments.Count == 0)
                return null;

            var inInlineScript = false;
            var openingBrace = default(Token);
            var closingBrace = default(Token);
            var openedBraces = 0;

            for (var i = 0; i < segments.Count; i++)
            {
                var token = segments[i];

                if (token.Kind == TokenKind.InlineScript)
                {
                    openingBrace = null;
                    closingBrace = null;
                    inInlineScript = true;
                }
                else if (inInlineScript)
                {
                    if (token.Kind == TokenKind.LCurly)
                    {
                        if (openingBrace == null)
                            openingBrace = token;

                        openedBraces++;
                    }
                    else if (token.Kind == TokenKind.RCurly)
                    {
                        openedBraces--;

                        if (openedBraces == 0)
                        {
                            closingBrace = token;
                            inInlineScript = false;
                        }
                    }
                }
            }

            if (openingBrace != null && position >= openingBrace.Extent.StartOffset)
            {
                var isInContext = false;

                if (closingBrace != null)
                {
                    if (position <= closingBrace.Extent.EndOffset)
                        isInContext = true;
                }
                else
                    isInContext = true;

                if (!isInContext)
                {
                    return null;
                }
            }
            else
                return null;

            return new TokenBlock
            {
                StartOffset = (openingBrace == null) ? 0 : openingBrace.Extent.StartOffset,
                EndOffset = (closingBrace == null) ? position : closingBrace.Extent.EndOffset
            };
        }

        public async Task<BracketSearchResult> FindBracketMatch(int position, TokenKind openToken, TokenKind closeToken, bool startWithClosedBracket = false)
        {
            return await Task.Run(() => {
                var segments = default(List<Token>);//_tokens.Where(item => item.Extent.EndOffset >= position || item.Extent.EndOffset == (position - 1)).ToList();
                var openingBrace = default(Token);
                var closingBrace = default(Token);
                var openedBraces = 0;

                if (_tokens == null)
                    return null;

                if (startWithClosedBracket)
                {
                    segments = _tokens.Where(item => item.Extent.StartOffset <= position).Reverse().ToList();
                }
                else
                {
                    segments =
                        _tokens.Where(item => item.Extent.EndOffset > position)// || item.Extent.EndOffset == (position - 1))
                            .ToList();
                }

                foreach (var token in segments)
                {
                    if (token.Kind == openToken)
                    {
                        if (startWithClosedBracket)
                            openedBraces--;
                        else
                            openedBraces++;

                        if ((openingBrace == null && !startWithClosedBracket) ||
                            (openedBraces == 0 && startWithClosedBracket))
                            openingBrace = token;
                    }

                    if (token.Kind == closeToken)
                    {
                        if (startWithClosedBracket)
                            openedBraces++;
                        else
                            openedBraces--;

                        if ((closingBrace == null && startWithClosedBracket) ||
                            (openedBraces == 0 && !startWithClosedBracket))
                            closingBrace = token;
                    }

                    if (openedBraces == 0)
                        break;
                }

                if (openingBrace == null || closingBrace == null)
                    return null;

                return new BracketSearchResult
                {
                    ClosingBracketOffset = closingBrace.Extent.StartOffset,
                    ClosingBracketLength = closingBrace.Extent.EndOffset - closingBrace.Extent.StartOffset,
                    OpeningBracketOffset = openingBrace.Extent.StartOffset,
                    OpeningBracketLength = openingBrace.Extent.EndOffset - openingBrace.Extent.StartOffset
                };
            });
        }

        /// <summary>
        /// Get a list of all runbook references found in this runbook
        /// </summary>
        /// <param name="runbookList">List of existing runbooks</param>
        /// <returns>List of referenced runbooks</returns>
        public List<RunbookModelProxy> GetReferences(IList<RunbookModelProxy> runbookList)
        {
            var references = runbookList.Where(item =>
                _tokens.FirstOrDefault(s =>
                    s.Extent.Text.Equals(item.RunbookName, StringComparison.InvariantCultureIgnoreCase)
                    && (s.Kind == TokenKind.Generic || s.Kind == TokenKind.Identifier)
                    && s.TokenFlags == TokenFlags.CommandName) != null)
                .ToList();

            return references;
        }

        /// <summary>
        /// Get the current token at the given position and line
        /// </summary>
        /// <param name="lineNumber">Line number to check</param>
        /// <param name="position">Position to check</param>
        /// <returns></returns>
        public List<Token> GetContext(int lineNumber, int position)
        {
            var tokenList = _tokens.Where(item => item.Extent.StartLineNumber == lineNumber 
                && (item.Extent.EndOffset <= position) || (position >= item.Extent.StartOffset && position <= item.Extent.EndOffset)).ToList();

            if (tokenList.Count == 0 && _tokens != null)
            {
                tokenList = _tokens.Where(item => item.Extent.StartLineNumber <= lineNumber 
                    && item.Extent.EndLineNumber >= lineNumber).ToList();

                return tokenList;
            }

            /*var currentStart = int.MinValue;
            var currentStop = int.MaxValue;
            var currentToken = default(Token);

            foreach (var token in tokenList)
            {
                if (token.Extent.StartOffset > currentStart && token.Extent.EndOffset < currentStop)
                {
                    currentStart = token.Extent.StartOffset;
                    currentStop = token.Extent.EndOffset;
                    currentToken = token;
                }
            }

            return currentToken;*/
            return tokenList;
        }

        /// <summary>
        /// Checks if the current position and line is within a string or comment
        /// </summary>
        /// <param name="lineNumber">Line to chcek</param>
        /// <param name="position">Position to check</param>
        /// <returns>True if it's within a string or false if not</returns>
        public bool IsWithinStringOrComment(int lineNumber, int position)
        {
            var context = GetContext(lineNumber, position);

            if (context == null)
                return false;

            foreach (var token in context)
            {
                switch (token.Kind)
                {
                    case TokenKind.StringExpandable:
                    case TokenKind.HereStringExpandable:
                    case TokenKind.Comment:
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Find the keyword from a given position (traverse backwards until a keyword is found)
        /// </summary>
        /// <param name="lineNumber">Line number to check</param>
        /// <param name="position">Position in the line</param>
        /// <returns>Token if found, null if not</returns>
        public Token GetKeywordFromPosition(int lineNumber, int position)
        {
            var tokens = _tokens.Where(item => (
                    item.Extent.StartLineNumber == lineNumber 
                    || item.Extent.EndLineNumber == lineNumber
                ) 
                && item.Extent.EndOffset < position).ToList();

            var keyword = tokens.LastOrDefault(item => (item.Kind == TokenKind.Generic || item.Kind == TokenKind.Identifier) && item.TokenFlags == TokenFlags.CommandName);

            return keyword;
        }

        /// <summary>
        /// Clears the cache
        /// </summary>
        public void ClearCache()
        {
            _tokens = null;
            _parseErrors = null;
        }

        public Token[] Tokens
        {
            get { return _tokens; }
        }

        public ScriptBlockAst ScriptBlock
        {
            get { return _scriptBlock; }
        }

        /// <summary>
        /// Gets or sets if the document is dirty (meaning it has changed and needs to be recalculated).
        /// </summary>
        public bool IsDirty { get; set; }

        /// <summary>
        /// Apply '$Using:' to variables defined outside current InlineScript scope
        /// </summary>
        /// <param name="variable"></param>
        /// <param name="position"></param>
        /// <param name="applyUsing"></param>
        /// <returns></returns>
        private string ApplyUsingStatement(Token variable, int position, bool applyUsing)
        {
            var tokenBlock = GetSubContext(position);

            if (tokenBlock == null)
                return variable.Text;

            if (variable.Extent.StartOffset >= tokenBlock.StartOffset && variable.Extent.EndOffset <= tokenBlock.EndOffset)
                return variable.Text;

            if (!variable.Text.StartsWith("$using:", StringComparison.InvariantCultureIgnoreCase) && applyUsing)
                return variable.Text.Replace("$", "$Using:");
            else if (variable.Text.StartsWith("$using:", StringComparison.InvariantCultureIgnoreCase) && !applyUsing)
                return variable.Text.Replace("$Using:", "$").Replace("$using:", "$").Replace("$USING:", "$");

            return variable.Text;
        }
    }
}
