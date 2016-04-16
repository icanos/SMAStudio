using Caliburn.Micro;
using Gemini.Framework;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using SMAStudiovNext.Core;
using SMAStudiovNext.Icons;
using SMAStudiovNext.Language.Snippets;
using SMAStudiovNext.Models;
using SMAStudiovNext.Modules.Runbook.Editor.Parser;
using SMAStudiovNext.Modules.Runbook.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Threading.Tasks;

namespace SMAStudiovNext.Modules.Runbook.Editor.Completion
{
    public class CompletionProviderOriginal : ICompletionProvider, IDisposable
    {
        /// <summary>
        /// Language parsing context
        /// </summary>
        private readonly LanguageContext _languageContext;
        private readonly IBackendContext _backendContext;

        private readonly PowerShell _powershell;

        /// <summary>
        /// List of keywords that is part of the Powershell language
        /// </summary>
        private readonly IList<string> _keywords = new List<string>
        {
            "Begin", "Break", "Catch", "Continue", "Data", "Do", "DynamicParam", "Else", "ElseIf", "End",
            "Exit", "Filter", "Finally", "For", "ForEach", "From", "Function", "If", "In", "InlineScript",
            "Hidden", "Parallel", "Param", "Process", "Return", "Sequence", "Switch", "Throw", "Trap", "Try",
            "Until", "While", "Workflow"
        };

        /// <summary>
        /// List of cmdlets that is part of SMA (but not resolved by native PS completion)
        /// </summary>
        private readonly IList<string> _smaCmdlets = new List<string>
        {
            "Get-AutomationVariable", "Get-AutomationPSCredential", "Get-AutomationCertificate", "Set-AutomationVariable", "Get-AutomationConnection"
        };

        /// <summary>
        /// This is set to true until a space or new line is added if we don't find a completion
        /// match for the word we're typing. This is to prevent the engine to look for matches
        /// even though we won't find anything.
        /// </summary>
        private bool _foundNoCompletions = false;

        /// <summary>
        /// Line number we're currently working with, this is used so that we know when the input
        /// continues on a new line (to flip _foundNoCompletions to false again).
        /// </summary>
        private int _cachedLineNumber = 0;

        /// <summary>
        /// Cached position in the line, this is also used to be able to flip the _foundNoCompletions flag
        /// if we're removing a letter or typing a new character.
        /// </summary>
        private int _cachedPosition = 0;

        private char? _cachedTriggerChar = null;

        public CompletionProviderOriginal(IBackendContext backendContext, LanguageContext languageContext)
        {
            _languageContext = languageContext;
            _backendContext = backendContext;

            _powershell = PowerShell.Create();
            _powershell.Runspace = RunspaceFactory.CreateRunspace();
            _powershell.Runspace.ThreadOptions = PSThreadOptions.UseNewThread;
            _powershell.Runspace.Open();

            Keywords = new List<ICompletionEntry>();
            Runbooks = new List<ICompletionEntry>();
        }

        public CompletionResult GetCompletionData(string completionWord, string content, string lineContent, DocumentLine line, int position, char? triggerChar)
        {
            var lineNumber = line.LineNumber;

            if (lineNumber == _cachedLineNumber && _foundNoCompletions && triggerChar == _cachedTriggerChar && position > _cachedPosition)
                return new CompletionResult(null);
            else if (position <= (_cachedPosition + 1))
                _foundNoCompletions = false;
            else if (position < _cachedPosition)
                _foundNoCompletions = false;
            else if (lineNumber != _cachedLineNumber)
                _foundNoCompletions = false;
            else if (triggerChar != _cachedTriggerChar)
                _foundNoCompletions = false;

            if (triggerChar == null)
                return new CompletionResult(null);

            // Make sure that we're not in a single line comment
            var commentPosition = lineContent.IndexOf("#");
            if (commentPosition > -1 && position > commentPosition)
                return new CompletionResult(null);

            // Make sure that we're not in a multi line comment (not sure how yet)
            
            _cachedLineNumber = lineNumber;
            _cachedPosition = position;
            _cachedTriggerChar = triggerChar;
            
            List<ICompletionData> completionData = null;

            completionData = new List<ICompletionData>();
            completionData.AddRange(GetPowershellCompletion(completionWord, content, position));
            /*switch (triggerChar)
            {
                case '$':
                    completionData.AddRange(GetVariableCompletion(completionWord, position));
                    break;
                case '-':
                    completionData.AddRange(GetParameterCompletion(completionWord, line.LineNumber, position));

                    if (completionData.Count < 1)
                        completionData.AddRange(GetPowershellCompletion(completionWord, content, position));
                    break;
                default:
                    double intCheck = 0;

                    // Everything
                    if (!double.TryParse(completionWord, out intCheck))
                    {
                        var snippetsCollection = AppContext.Resolve<ISnippetsCollection>();
                        completionData.AddRange(snippetsCollection.Snippets.Where(item => item.Name.StartsWith(completionWord, StringComparison.InvariantCultureIgnoreCase)).Select(item => new SnippetCompletionData(item)));

                        //completionData.AddRange(_keywords.Where(item => item.StartsWith(completionWord, StringComparison.InvariantCultureIgnoreCase)).Select(item => new KeywordCompletionData(item, Glyph.Keyword)));
                        completionData.AddRange(_backendContext.Runbooks.Where(item => (item.Tag as RunbookModelProxy).RunbookName.Contains(completionWord)).Select(item => new KeywordCompletionData((item.Tag as RunbookModelProxy).RunbookName, Glyph.ClassPublic)));
                        completionData.AddRange(_smaCmdlets.Where(item => item.StartsWith(completionWord, StringComparison.InvariantCultureIgnoreCase)).Select(item => new KeywordCompletionData(item, Glyph.MethodPublic)));
                    }

                    completionData.AddRange(GetPowershellCompletion(completionWord, content, position));
                    break;
            }*/

            completionData = completionData.OrderBy(item => item.Text).ToList();

            return new CompletionResult(completionData);
            //});
        }

        private IList<VariableCompletionData> GetVariableCompletion(string completionWord, int caretOffset)
        {
            if (_languageContext.IsInSubContext(caretOffset))
                return _languageContext.GetVariables(caretOffset, true).ToList();

            return _languageContext.GetVariables(caretOffset, false).ToList();
        }

        private IList<ICompletionData> GetParameterCompletion(string completionWord, int lineNumber, int caretOffset)
        {
            var completionData = new List<ICompletionData>();
            // Parameter
            var keyword = _languageContext.GetKeywordFromPosition(lineNumber, caretOffset);

            if (keyword != null && _backendContext.IsRunbook(keyword.Text))
            {
                var statusManager = AppContext.Resolve<IStatusManager>();
                statusManager.SetText("Loading parameters from " + keyword.Text);

                var runbook = GetRunbook(keyword.Text);
                if (runbook != null)
                {
                    var parameters = runbook.GetParameters(completionWord);

                    completionData.AddRange(parameters);
                    statusManager.SetTimeoutText("Parameters loaded.", 5);
                }
            }

            return completionData;
        }

        private IList<ICompletionData> GetPowershellCompletion(string completionWord, string content, int caretOffset)
        {
            var completionData = new List<ICompletionData>();

            // Get built in PS completion
            try {
                Debug.WriteLine("GetPowershellCompletion(" + completionWord + ", [content truncated], " + caretOffset + ")");

                /*var result =
                    CommandCompletion.CompleteInput(
                        content, caretOffset, null, _powershell).CompletionMatches;
                
                */
                Ast ast;
                Token[] tokens;
                IScriptPosition cursorPosition;

                GetCommandCompletionParameters(content, caretOffset, out ast, out tokens, out cursorPosition);

                if (ast == null)
                {
                    return null;
                }

                CommandCompletion commandCompletion = null;

                /*if (runspace.RunspaceAvailability == RunspaceAvailability.Available)
                {
                    using (_currentPowerShell = PowerShell.Create())
                    {
                        _currentPowerShell.Runspace = runspace;
                        commandCompletion = CommandCompletion.CompleteInput(ast, tokens, cursorPosition, null, _currentPowerShell);
                    }
                }*/
                commandCompletion = CommandCompletion.CompleteInput(ast, tokens, cursorPosition, null, _powershell);

                foreach (var item in commandCompletion.CompletionMatches)
                {
                    switch (item.ResultType)
                    {
                        case CompletionResultType.Type:
                        case CompletionResultType.Keyword:
                            completionData.Add(new KeywordCompletionData(item.CompletionText, Glyph.Keyword, item.ToolTip));
                            break;
                        case CompletionResultType.Command:
                            completionData.Add(new KeywordCompletionData(item.CompletionText, Glyph.MethodPublic, item.ToolTip));
                            break;
                        case CompletionResultType.ParameterName:
                            completionData.Add(new ParameterCompletionData(item.CompletionText, string.Empty, item.ToolTip));
                            break;
                        case CompletionResultType.ParameterValue:
                            completionData.Add(new ParameterValueCompletionData(item.CompletionText, item.ToolTip));
                            break;
                        case CompletionResultType.Property:
                            completionData.Add(new ParameterCompletionData(item.CompletionText, string.Empty, item.ToolTip, false));
                            break;
                        case CompletionResultType.Variable:
                            completionData.Add(new VariableCompletionData(item.CompletionText, string.Empty));
                            break;
                    }
                }

            }
            catch (PSInvalidOperationException ex)
            {
                Logger.Debug("Error when trying to code complete.", ex);
            }

            return completionData;
        }

        /// <summary>
        /// Get the abstract syntax tree, tokens and the cursor position.
        /// </summary>
        /// <param name="script">The active script.</param>
        /// <param name="caretPosition">The caret position.</param>
        /// <param name="ast">The AST to get.</param>
        /// <param name="tokens">The tokens to get.</param>
        /// <param name="cursorPosition">The cursor position to get.</param>
        public void GetCommandCompletionParameters(string script, int caretPosition, out Ast ast, out Token[] tokens, out IScriptPosition cursorPosition)
        {
            ParseError[] array;
            ast = Tokenize(script, out tokens, out array);
            if (ast != null)
            {
                //HACK: Clone with a new offset using private method... 
                var type = ast.Extent.StartScriptPosition.GetType();
                var method = type.GetMethod("CloneWithNewOffset",
                                            BindingFlags.Instance | BindingFlags.NonPublic,
                                            null,
                                            new[] { typeof(int) }, null);

                cursorPosition = (IScriptPosition)method.Invoke(ast.Extent.StartScriptPosition, new object[] { caretPosition });
                return;
            }
            cursorPosition = null;
        }

        /// <summary>
        /// Tokonize the script and get the needed data.
        /// </summary>
        /// <param name="script">The active script.</param>
        /// <param name="tokens">The tokens to get.</param>
        /// <param name="errors">The parse errors to get.</param>
        /// <returns></returns>
        public Ast Tokenize(string script, out Token[] tokens, out ParseError[] errors)
        {
            Ast result;

            try
            {
                Ast ast = System.Management.Automation.Language.Parser.ParseInput(script, out tokens, out errors);
                result = ast;
            }
            catch (RuntimeException ex)
            {
                var parseError = new ParseError(null, ex.ErrorRecord.FullyQualifiedErrorId, ex.Message);
                errors = new[] { parseError };
                tokens = new Token[0];
                result = null;
            }

            return result;
        }

        /// <summary>
        /// Finds the actual runbook object
        /// </summary>
        /// <param name="runbookName"></param>
        /// <returns></returns>
        private RunbookViewModel GetRunbook(string runbookName)
        {
            var application = IoC.Get<IModule>();
            var contexts = (application as Modules.Startup.Module).GetContexts();
            var runbook = default(ResourceContainer);

            foreach (var context in contexts)
            {
                runbook = context.Runbooks.FirstOrDefault(item => (item.Tag as RunbookModelProxy).RunbookName.Equals(runbookName, StringComparison.InvariantCultureIgnoreCase));

                if (runbook != null)
                    break;
            }

            if (runbook == null)
                return null;

            return (runbook.Tag as RunbookModelProxy).GetViewModel<RunbookViewModel>();
        }

        public LanguageContext Context
        {
            get { return _languageContext; }
        }

        public IList<ICompletionEntry> Keywords { get; set; }

        public IList<ICompletionEntry> Runbooks { get; set; }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _powershell.Runspace.Close();
                    _powershell.Runspace.Dispose();
                }
                
                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~CompletionProvider() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
