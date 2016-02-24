using ICSharpCode.AvalonEdit.CodeCompletion;
using SMAStudio.Language;
using SMAStudiovNext.Core;
using SMAStudiovNext.Icons;
using SMAStudiovNext.Language.Snippets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Language.Completion
{
    public class CompletionProvider : ICompletionProvider
    {
        private readonly LanguageContext _languageContext;
        private readonly IList<string> _keywords = new List<string>
        {
            "Begin", "Break", "Catch", "Continue", "Data", "Do", "DynamicParam", "Else", "ElseIf", "End",
            "Exit", "Filter", "Finally", "For", "ForEach", "From", "Function", "If", "In", "InlineScript",
            "Hidden", "Parallel", "Param", "Process", "Return", "Sequence", "Switch", "Throw", "Trap", "Try",
            "Until", "While", "Workflow"
        };

        private readonly IList<string> _smaCmdlets = new List<string>
        {
            "Get-AutomationVariable", "Get-AutomationPSCredential", "Get-AutomationCertificate", "Set-AutomationVariable", "Get-AutomationConnection"
        };

        public CompletionProvider()
        {
            _languageContext = new LanguageContext();

            Keywords = new List<ICompletionEntry>();
            Runbooks = new List<ICompletionEntry>();
        }

        public async Task<CompletionResult> GetCompletionData(string completionWord, string line, int position, char? triggerChar)
        {
            var contextTree = _languageContext.GetContext(position);
            var context = contextTree.FirstOrDefault();
            
            return await Task.Run(() =>
            {
                List<ICompletionData> completionData = null;
                bool includeNativePowershell = false;

                if (triggerChar != null)
                {
                    //if (!char.IsLetterOrDigit(triggerChar.Value))
                    //    return null;

                    completionData = new List<ICompletionData>();

                    switch (triggerChar.Value)
                    {
                        case '.':
                            // Properties
                            includeNativePowershell = true;
                            break;
                        case '-':
                            // Parameters
                            includeNativePowershell = true;
                            completionData.AddRange(_smaCmdlets.Where(item => item.StartsWith(completionWord, StringComparison.InvariantCultureIgnoreCase)).Select(item => new KeywordCompletionData(item, IconsDescription.Cmdlet)));
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
                                includeNativePowershell = false;

                                completionData.AddRange(_keywords.Select(item => new KeywordCompletionData(item, IconsDescription.LanguageConstruct)));
                            }
                            else
                            {
                                // We add the native powershell cmdlets when the first dash is typed instead (minimizing lag)
                                includeNativePowershell = true;

                                completionData.AddRange(_languageContext.GetVariables().Where(item => item.Text.StartsWith(completionWord, StringComparison.InvariantCultureIgnoreCase)).Distinct().ToList());
                                completionData.AddRange(Runbooks.Where(item => item.Name.Contains(completionWord)).Select(item => new KeywordCompletionData(item.DisplayText, IconsDescription.Runbook)).ToList());
                            }

                            var snippetsCollection = AppContext.Resolve<ISnippetsCollection>();
                            completionData.AddRange(snippetsCollection.Snippets.Where(item => item.Name.StartsWith(completionWord)).Select(item => new SnippetCompletionData(item)));
                            break;
                    }

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
                                case System.Management.Automation.CompletionResultType.Property:
                                    completionData.Add(new ParameterCompletionData(item.CompletionText, string.Empty, item.ToolTip, false));
                                    break;
                            }
                        }
                    }

                    completionData = completionData.OrderBy(item => item.Text).ToList();
                }

                return new CompletionResult(completionData);
            });
        }

        public LanguageContext Context
        {
            get { return _languageContext; }
        }

        public IList<ICompletionEntry> Keywords { get; set; }

        public IList<ICompletionEntry> Runbooks { get; set; }
    }
}
