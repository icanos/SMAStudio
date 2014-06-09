using SMAStudio.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Management.Automation.Language;
using SMAStudio.Resources;
using SMAStudio.Models;

namespace SMAStudio.Editor.Parsing
{
    /// <summary>
    /// Async service that analyses the code in the currently active window
    /// and runs that through the Powershell Parser in order to find any
    /// parsing errors, and in that case, notify the user of them.
    /// </summary>
    sealed class ParserService : IDisposable
    {
        private bool _running = true;
        private object _sync = new object();

        private Thread _thread = null;

        private WorkspaceViewModel _workspaceViewModel;
        private ErrorListViewModel _errorListViewModel;

        public ParserService(WorkspaceViewModel workspaceViewModel, ErrorListViewModel errorListViewModel)
        {
            _workspaceViewModel = workspaceViewModel;
            _errorListViewModel = errorListViewModel;
        }

        public void Start()
        {
            _thread = new Thread(new ThreadStart(delegate()
            {
                Token[] tokens;
                ParseError[] parseErrors;

                while (_running)
                {
                    string scriptContent = string.Empty;
                    IDocumentViewModel document = null;

                    lock (_sync)
                    {
                        document = _workspaceViewModel.CurrentDocument;
                        if (document == null)
                        {
                            Thread.Sleep(2 * 1000);
                            continue;
                        }

                        if (!(document is RunbookViewModel))
                        {
                            Thread.Sleep(2 * 1000);
                            continue;
                        }

                        if ((DateTime.Now - document.LastTimeKeyDown) < TimeSpan.FromSeconds(1))
                        {
                            Thread.Sleep(2 * 1000);
                            continue;
                        }

                        scriptContent = document.Content;
                    }

                    Parser.ParseInput(scriptContent, out tokens, out parseErrors);

                    if (parseErrors.Length > 0)
                    {
                        document.Icon = Icons.ParseError;
                        _workspaceViewModel.StatusBarText = parseErrors[0].Message;

                        _errorListViewModel.RemoveFixedErrors(parseErrors, ((RunbookViewModel)document).RunbookName);

                        foreach (var error in parseErrors)
                        {
                            var errItem = new ErrorListItem
                            {
                                ErrorId = error.ErrorId,
                                LineNumber = error.Extent.StartLineNumber,
                                Description = error.Message,
                                Runbook = ((RunbookViewModel)document).RunbookName
                            };

                            _errorListViewModel.AddItem(errItem);
                        }

                        // TODO: Colorize the error in the editor
                    }
                    else if (parseErrors.Length == 0 && document.Icon.Equals(Icons.ParseError))
                    {
                        document.Icon = Icons.Runbook;

                        _errorListViewModel.RemoveErrorByRunbook(((RunbookViewModel)document).RunbookName);
                    }

                    parseErrors = null;
                    tokens = null;

                    Thread.Sleep(2 * 1000);
                }
            }));

            _thread.IsBackground = true;
            _thread.Start();
        }

        public void Dispose()
        {
            _running = false;

            try
            {
                _thread.Abort();
            }
            catch (ThreadAbortException)
            {

            }
        }
    }
}
