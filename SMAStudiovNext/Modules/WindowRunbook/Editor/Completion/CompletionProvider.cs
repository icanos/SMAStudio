using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.AvalonEdit.Document;
using SMAStudiovNext.Modules.Runbook.Editor.Parser;
using SMAStudiovNext.Core;
using System.Management.Automation.Runspaces;
using System.Management.Automation;
using ICSharpCode.AvalonEdit.CodeCompletion;
using SMAStudiovNext.Models;
using System.Management.Automation.Language;
using SMAStudiovNext.Modules.Runbook.ViewModels;
using SMAStudiovNext.Language.Snippets;

namespace SMAStudiovNext.Modules.Runbook.Editor.Completion
{
    public delegate void CompletionResultDelegate(object sender, CompletionEventArgs e);

    public class CompletionProvider : ICompletionProvider
    {
        /// <summary>
        /// Language parsing context
        /// </summary>
        private readonly LanguageContext _languageContext;
        private readonly IBackendContext _backendContext;
        private readonly ISnippetsCollection _snippetsCollection;

        /// <summary>
        /// List of cmdlets that is part of SMA (but not resolved by native PS completion)
        /// </summary>
        private readonly IList<string> _smaCmdlets = new List<string>
        {
            "Get-AutomationVariable", "Get-AutomationPSCredential", "Get-AutomationCertificate", "Set-AutomationVariable", "Get-AutomationConnection"
        };

        private long _requestTrigger;
        private readonly Runspace _runspace;
        private readonly object _syncLock = new object();

        public event CompletionResultDelegate OnCompletionCompleted;

        public CompletionProvider(IBackendContext backendContext, LanguageContext languageContext)
        {
            _languageContext = languageContext;
            _backendContext = backendContext;
            _snippetsCollection = AppContext.Resolve<ISnippetsCollection>();

            _runspace = RunspaceFactory.CreateRunspace();
            _runspace.Open();
        }

        public LanguageContext Context
        {
            get
            {
                return _languageContext;
            }
        }

        /*public IList<ICompletionEntry> Keywords
        {
            get; set;
        }

        public IList<ICompletionEntry> Runbooks
        {
            get; set;
        }*/

        /// <summary>
        /// Initializes the Powershell code completion engine, since it takes quite a while
        /// for the first completion to complete.
        /// </summary>
        public void Initialize()
        {
            CommandCompletionHelper.GetCommandCompletionList("Write-", 6, _runspace);
        }

        /// <summary>
        /// Retrieve completion suggestions
        /// </summary>
        /// <param name="completionWord">Word to complete</param>
        /// <param name="content">Script that we're working with</param>
        /// <param name="lineContent">Content of the current line</param>
        /// <param name="line">Line object from AvaloneEdit</param>
        /// <param name="position">Caret offset</param>
        /// <param name="triggerChar">Not used</param>
        /// <param name="triggerTag">Counter</param>
        public void GetCompletionData(string completionWord, string content, string lineContent, DocumentLine line, int position, char? triggerChar, long triggerTag)
        {
            if (_requestTrigger != 0 && triggerTag <= _requestTrigger)
                return;

            DismissGetCompletionResults();
            ProcessCompletion(content, completionWord, position, triggerTag);
        }

        /// <summary>
        /// Retrieve parameter suggestions from runbooks
        /// </summary>
        /// <param name="token">Runbook to get suggestions from</param>
        /// <param name="completionWord">Word to complete</param>
        /// <param name="triggerTag">Counter</param>
        public void GetParameterCompletionData(Token token, string completionWord, long triggerTag)
        {
            if (_requestTrigger > 0 || _requestTrigger > triggerTag)
                return;

            lock (_syncLock)
            {
                _requestTrigger = triggerTag;
            }

            Task.Run(() =>
            {
                var runbook = _backendContext.Runbooks.FirstOrDefault(item => (item.Tag as RunbookModelProxy).RunbookName.Equals(token.Text, StringComparison.InvariantCultureIgnoreCase));

                if (runbook == null)
                    return;

                var runbookViewModel = (runbook.Tag as RunbookModelProxy).GetViewModel<RunbookViewModel>();
                var parameters = runbookViewModel.GetParameters(completionWord);

                var completionData = parameters.ToList();

                OnCompletionCompleted?.Invoke(this, new CompletionEventArgs(completionData));

                // Reset trigger
                lock (_syncLock)
                {
                    _requestTrigger = 0;
                }
            });
        }

        /// <summary>
        /// Check if the provided token is a runbook or not
        /// </summary>
        /// <param name="token">Token to check</param>
        /// <returns>True if runbook, false if not</returns>
        public bool IsRunbook(Token token)
        {
            return (_backendContext.Runbooks.FirstOrDefault(item => (item.Tag as RunbookModelProxy).RunbookName.Equals(token.Text, StringComparison.InvariantCultureIgnoreCase)) != null);
        }

        /// <summary>
        /// Processes a completion request by sending it to the powershell completion engine.
        /// </summary>
        /// <param name="content">Script to complete</param>
        /// <param name="completionWord">Word to complete</param>
        /// <param name="position">Caret offset</param>
        /// <param name="triggerTag">Counter</param>
        private void ProcessCompletion(string content, string completionWord, int position, long triggerTag)
        {
            lock (_syncLock)
            {
                _requestTrigger = triggerTag;
            }

            Task.Run(() =>
            {
                try
                {
                    CommandCompletion commandCompletion = null;

                    lock (_syncLock)
                    {
                        if (_runspace.RunspaceAvailability == RunspaceAvailability.Available)
                        {
                            commandCompletion = CommandCompletionHelper.GetCommandCompletionList(content, position, _runspace);
                        }
                    }

                    var completionData = new List<ICompletionData>();

                    if (!string.IsNullOrEmpty(completionWord))
                    {
                        // Add snippets
                        completionData.AddRange(_snippetsCollection.Snippets.Where(item => item.Name.StartsWith(completionWord, StringComparison.InvariantCultureIgnoreCase)).Select(item => new SnippetCompletionData(item)));

                        // Add SMA cmdlets
                        completionData.AddRange(_smaCmdlets.Where(item => item.StartsWith(completionWord, StringComparison.InvariantCultureIgnoreCase)).Select(item => new KeywordCompletionData(item)));

                        // Add runbooks matching the completion word
                        completionData.AddRange(_backendContext.Runbooks.Where(item => (item.Tag as RunbookModelProxy).RunbookName.StartsWith(completionWord, StringComparison.InvariantCultureIgnoreCase)).Select(item => new KeywordCompletionData((item.Tag as RunbookModelProxy).RunbookName)).ToList());

                    }

                    if (commandCompletion == null)
                        return;

                    // Add powershell completions
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

                    // Is this inferring performance penalties?
                    completionData = completionData.OrderBy(item => item.Text).ToList();

                    OnCompletionCompleted?.Invoke(this, new CompletionEventArgs(completionData));

                    // Reset trigger
                    lock (_syncLock)
                    {
                        _requestTrigger = 0;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Failed to retrieve the completion list per request due to exception: " + ex.Message, ex);
                }
            });
        }

        /// <summary>
        /// Dismiss the current running completion request
        /// </summary>
        private static void DismissGetCompletionResults()
        {
            try
            {
                CommandCompletionHelper.DismissCommandCompletionListRequest();
            }
            catch
            {
                Logger.DebugFormat("Failed to stop existing completion.");
                //ServiceCommon.Log("Failed to stop the existing one.");
            }
        }
    }
}
