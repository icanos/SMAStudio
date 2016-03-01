using Caliburn.Micro;
using Gemini.Framework;
using Gemini.Framework.Services;
using ICSharpCode.AvalonEdit.CodeCompletion;
using SMAStudio.Language;
using SMAStudiovNext.Core;
using SMAStudiovNext.Icons;
using SMAStudiovNext.Language.Snippets;
using SMAStudiovNext.Models;
using SMAStudiovNext.Modules.Runbook.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SMAStudiovNext.Language.Completion
{
    public class CompletionProvider : ICompletionProvider
    {
        /// <summary>
        /// Language parsing context
        /// </summary>
        private readonly LanguageContext _languageContext;
        private readonly IBackendContext _backendContext;

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

        public CompletionProvider(IBackendContext backendContext, LanguageContext languageContext)
        {
            _languageContext = languageContext;
            _backendContext = backendContext;

            Keywords = new List<ICompletionEntry>();
            Runbooks = new List<ICompletionEntry>();
        }

        public async Task<CompletionResult> GetCompletionData(string completionWord, string line, int lineNumber, int position, char? triggerChar)
        {
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

            var contextTree = _languageContext.GetContext(lineNumber, position);
            var context = contextTree.FirstOrDefault();

            if (context != null && (context.Type == ExpressionType.QuotedString || context.Type == ExpressionType.SingleQuotedString || context.Type == ExpressionType.Comment || context.Type == ExpressionType.MultilineComment))
                return new CompletionResult(null);

            _cachedLineNumber = lineNumber;
            _cachedPosition = position;
            _cachedTriggerChar = triggerChar;
            
            return await Task.Run(() =>
            {
                List<ICompletionData> completionData = null;
                bool includeNativePowershell = false;

                if (context != null && (context.Type == ExpressionType.QuotedString || context.Type == ExpressionType.SingleQuotedString))
                    return new CompletionResult(null);

                if (triggerChar != null)
                {
                    completionData = new List<ICompletionData>();

                    switch (triggerChar.Value)
                    {
                        case '.':
                            // Properties
                            includeNativePowershell = true;
                            break;
                        case '-':
                            // Parameters
                            var runbookContext = contextTree.FirstOrDefault(item => item.Type == ExpressionType.Keyword);
                            if (runbookContext != null)
                            {
                                //var runbookComplete = Runbooks.FirstOrDefault(item => item.Name.Equals(runbookContext.Value, StringComparison.InvariantCultureIgnoreCase));
                                var runbookComplete = _backendContext.Runbooks.FirstOrDefault(item => (item.Tag as RunbookModelProxy).RunbookName.Equals(runbookContext.Value, StringComparison.InvariantCultureIgnoreCase));

                                if (runbookComplete != null)
                                {
                                    var shell = IoC.Get<IShell>();
                                    var statusManager = AppContext.Resolve<IStatusManager>();
                                    statusManager.SetText("Loading parameters from " + (runbookComplete.Tag as RunbookModelProxy).RunbookName);
                                    //Shell.StatusBar.Items[0].Message = "";

                                    var runbook = GetRunbook((runbookComplete.Tag as RunbookModelProxy).RunbookName);

                                    if (runbook != null)
                                    {
                                        completionData.AddRange(runbook.GetParameters(completionWord));
                                        statusManager.SetTimeoutText("Parameters loaded.", 5);
                                    }
                                }
                                else
                                {
                                    includeNativePowershell = true;
                                    completionData.AddRange(_smaCmdlets.Where(item => item.StartsWith(completionWord, StringComparison.InvariantCultureIgnoreCase)).Select(item => new KeywordCompletionData(item, IconsDescription.Cmdlet)));
                                }
                            }
                            else
                            {
                                includeNativePowershell = true;
                                completionData.AddRange(_smaCmdlets.Where(item => item.StartsWith(completionWord, StringComparison.InvariantCultureIgnoreCase)).Select(item => new KeywordCompletionData(item, IconsDescription.Cmdlet)));
                            }
                            break;
                        case ':':
                            // Variables or .NET assembly
                            if (line.EndsWith("::"))
                            {
                                // .NET type
                                includeNativePowershell = true;
                            }
                            else if (!line.EndsWith("]:"))
                            {
                                if (context != null && context.Value.Equals("$using:", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    completionData.AddRange(_languageContext.GetVariables(true));
                                }
                                else
                                    completionData.AddRange(_languageContext.GetVariables());
                            }
                            break;
                        case '$':
                            if (_languageContext.IsInSubContext(position))
                            {
                                // Inside an inline script, add $Using: to each variable
                                completionData.AddRange(_languageContext.GetVariables(true));
                            }
                            else
                                completionData.AddRange(_languageContext.GetVariables());
                            break;
                        case '{':
                        case '}':
                        case '"':
                        case '\'':
                        case '[':
                        case ']':
                            // Ignore characters
                            break;
                        default:
                            // all
                            var foundInLang = _keywords.FirstOrDefault(item => item.StartsWith(completionWord));

                            if (foundInLang != null)
                            {
                                includeNativePowershell = true;

                                completionData.AddRange(_keywords.Select(item => new KeywordCompletionData(item, IconsDescription.LanguageConstruct)));
                            }
                            else
                            {
                                // COMMENTED: We add the native powershell cmdlets when the first dash is typed instead (minimizing lag)
                                //includeNativePowershell = true;

                                completionData.AddRange(_languageContext.GetVariables().Where(item => item.Text.StartsWith(completionWord, StringComparison.InvariantCultureIgnoreCase)).Distinct().ToList());
                                completionData.AddRange(_backendContext.Runbooks.Where(item => (item.Tag as RunbookModelProxy).RunbookName.Contains(completionWord)).Select(item => new KeywordCompletionData((item.Tag as RunbookModelProxy).RunbookName, IconsDescription.Runbook)));
                            }

                            var snippetsCollection = AppContext.Resolve<ISnippetsCollection>();
                            completionData.AddRange(snippetsCollection.Snippets.Where(item => item.Name.StartsWith(completionWord)).Select(item => new SnippetCompletionData(item)));
                            break;
                    }

                }
                else
                    includeNativePowershell = true;

                if (includeNativePowershell)
                {
                    // Get built in PS completion
                    var ret = System.Management.Automation.CommandCompletion.MapStringInputToParsedInput(line, line.Length);
                    var candidates =
                        System.Management.Automation.CommandCompletion.CompleteInput(
                            ret.Item1, ret.Item2, ret.Item3, null,
                            System.Management.Automation.PowerShell.Create()
                        ).CompletionMatches;

                    var result = candidates.Where(item =>
                            item.ResultType == System.Management.Automation.CompletionResultType.Command ||
                            item.ResultType == System.Management.Automation.CompletionResultType.ParameterName ||
                            item.ResultType == System.Management.Automation.CompletionResultType.ParameterValue ||
                            item.ResultType == System.Management.Automation.CompletionResultType.Property ||
                            item.ResultType == System.Management.Automation.CompletionResultType.Type ||
                            item.ResultType == System.Management.Automation.CompletionResultType.Keyword
                        ).ToList();

                    foreach (var item in result)
                    {
                        switch (item.ResultType)
                        {
                            case System.Management.Automation.CompletionResultType.Type:
                            case System.Management.Automation.CompletionResultType.Keyword:
                                completionData.Add(new KeywordCompletionData(item.CompletionText, IconsDescription.LanguageConstruct, item.ToolTip));
                                break;
                            case System.Management.Automation.CompletionResultType.Command:
                                completionData.Add(new KeywordCompletionData(item.CompletionText, IconsDescription.Cmdlet, item.ToolTip));
                                break;
                            case System.Management.Automation.CompletionResultType.ParameterName:
                                completionData.Add(new ParameterCompletionData(item.CompletionText, string.Empty, item.ToolTip));
                                break;
                            case System.Management.Automation.CompletionResultType.ParameterValue:
                                completionData.Add(new ParameterValueCompletionData(item.CompletionText, item.ToolTip));
                                break;
                            case System.Management.Automation.CompletionResultType.Property:
                                completionData.Add(new ParameterCompletionData(item.CompletionText, string.Empty, item.ToolTip, false));
                                break;
                        }
                    }
                }

                if (completionData.Count == 0)
                    _foundNoCompletions = true;

                completionData = completionData.OrderBy(item => item.Text).ToList();

                return new CompletionResult(completionData);
            });
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
    }
}
