using Caliburn.Micro;
using Gemini.Framework;
using Gemini.Framework.Services;
using Gemini.Modules.ErrorList;
using Gemini.Modules.Output;
using SMAStudiovNext.Core;
using SMAStudiovNext.Icons;
using SMAStudiovNext.Language.Completion;
using SMAStudiovNext.Models;
using SMAStudiovNext.Modules.Runbook.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Management.Automation.Language;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SMAStudiovNext.Agents
{
    /// <summary>
    /// Checks the currently active runbook for parse errors and populates the error list
    /// in case of errors AND parses the runbook in order to provide auto completion for our auto completion
    /// agent.
    /// </summary>
    public class RunbooksScanningAgent : IAgent
    {
        private readonly IErrorList _errorList;
        private readonly IOutput _output;
        private readonly IShell _shell;

        private readonly object _syncLock = new object();
        private readonly Thread _backgroundThread;
        private bool _isRunning = true;

        private readonly IDictionary<string, int> _errorStates;

        public RunbooksScanningAgent()
        {
            _errorStates = new Dictionary<string, int>();

            _backgroundThread = new Thread(new ThreadStart(StartInternal));
            _backgroundThread.Priority = ThreadPriority.BelowNormal;

            //_completionProvider = AppContext.Resolve<ICompletionProvider>();
            _errorList = IoC.Get<IErrorList>();
            _output = IoC.Get<IOutput>();
            _shell = IoC.Get<IShell>();
        }

        /// <summary>
        /// Entry point for agent
        /// </summary>
        public void Start()
        {
            _backgroundThread.Start();
        }

        /// <summary>
        /// Runs our background thread where the parsing of the runbook is done. If there is parse errors in our runbook,
        /// tokens will be null and we won't be able to populate our auto complete engine with information about variables etc.
        /// </summary>
        private void StartInternal()
        {
            Token[] tokens;
            ParseError[] parseErrors;

            while (_isRunning)
            {
                if (_shell.ActiveItem != null && (_shell.ActiveItem is RunbookViewModel))
                {
                    var content = ((RunbookViewModel)_shell.ActiveItem).Content;

                    // Run the script through the PS parsers
                    var scriptBlock = System.Management.Automation.Language.Parser.ParseInput(content, out tokens, out parseErrors);

                    if (_errorList != null)
                        ParseRunbookForErrors(parseErrors);

                    if (tokens != null && tokens.Length > 0)
                    {
                        if (_shell.ActiveItem is RunbookViewModel)
                        {
                            (_shell.ActiveItem as RunbookViewModel).ParseContent();
                        }
                    }
                }

                var contexts = ((Modules.Startup.Module)IoC.Get<IModule>()).GetContexts();
                var runbookHash = 0;
                var lastRunbookHash = 0;

                //_completionProvider.Runbooks.Clear();

                for (var i = 0; i < contexts.Count; i++)
                {
                    var context = contexts[i];

                    if (context.Runbooks != null)
                    {
                        var runbooks = context.Runbooks;

                        foreach (var runbook in runbooks)
                            runbookHash += ((RunbookModelProxy)runbook.Tag).RunbookName.Length;

                        if (runbookHash.Equals(lastRunbookHash))
                        {
                            Thread.Sleep(5 * 1000);
                            continue;
                        }

                        // TODO: Restore this somehow!
                        //foreach (var runbook in runbooks)
                        //{
                        //    _completionProvider.Runbooks.Add(new KeywordCompletionData(((RunbookModelProxy)runbook.Tag).RunbookName, IconsDescription.Runbook));
                        //}
                    }

                    lastRunbookHash = runbookHash;
                }

                Thread.Sleep(2 * 1000);
            }
        }
        
        /// <summary>
        /// Handles notifying the user that there is somekind of parse error in the runbook.
        /// </summary>
        private void ParseRunbookForErrors(ParseError[] parseErrors)
        {
            bool hasNewErrors = true;
            var hashCode = 0;
            foreach (var error in parseErrors)
                hashCode += error.Message.Length + error.Extent.StartLineNumber + error.Extent.StartColumnNumber;

            if (_errorStates.ContainsKey(_shell.ActiveItem.ContentId))
            {
                if (hashCode.Equals(_errorStates[_shell.ActiveItem.ContentId]))
                    hasNewErrors = false;
            }

            if (parseErrors.Length > 0 && hasNewErrors)
            {
                NotifyParseErrors((RunbookViewModel)_shell.ActiveItem, parseErrors, hashCode);
            }
            else if (parseErrors.Length == 0 && _errorStates.ContainsKey(_shell.ActiveItem.ContentId) && _errorStates[_shell.ActiveItem.ContentId] > 0)
            {
                ClearParseErrors((RunbookViewModel)_shell.ActiveItem);
            }
        }

        /// <summary>
        /// Populates the error list with errors
        /// </summary>
        /// <param name="runbookViewModel"></param>
        /// <param name="parseErrors"></param>
        /// <param name="hashCode"></param>
        private void NotifyParseErrors(RunbookViewModel runbookViewModel, ParseError[] parseErrors, int hashCode)
        {
            ClearParseErrors(runbookViewModel);

            foreach (var error in parseErrors)
            {
                _errorList.AddItem(
                    ErrorListItemType.Error,
                    error.Message,
                    runbookViewModel.DisplayName,
                    error.Extent.StartLineNumber,
                    error.Extent.StartColumnNumber
                );
            }

            if (_errorStates.ContainsKey(runbookViewModel.ContentId))
                _errorStates[runbookViewModel.ContentId] = hashCode;
            else
                _errorStates.Add(runbookViewModel.ContentId, hashCode);
        }

        /// <summary>
        /// Clears errors from the error list
        /// </summary>
        /// <param name="runbookViewModel"></param>
        private void ClearParseErrors(RunbookViewModel runbookViewModel)
        {
            var errors = _errorList.Items;
            var toRemove = new List<ErrorListItem>();

            foreach (var error in errors)
            {
                if (error.Path.Equals(runbookViewModel.DisplayName))
                    toRemove.Add(error);
            }

            _errorList.Items.RemoveRange(toRemove);

            if (_errorStates.ContainsKey(runbookViewModel.ContentId))
            {
                _errorStates[runbookViewModel.ContentId] = 0;
            }
        }

        /// <summary>
        /// Called when the agent should stop
        /// </summary>
        public void Stop()
        {
            lock (_syncLock)
            {
                _isRunning = true;
                _backgroundThread.Abort();
            }
        }
    }
}
