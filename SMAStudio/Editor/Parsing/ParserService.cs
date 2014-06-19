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
using Microsoft.Practices.Unity;

namespace SMAStudio.Editor.Parsing
{
    /// <summary>
    /// Async service that analyses the code in the currently active window
    /// and runs that through the Powershell Parser in order to find any
    /// parsing errors, and in that case, notify the user of them.
    /// </summary>
    sealed class ParserService : IParserService, IDisposable
    {
        private bool _running = true;
        private object _sync = new object();

        private Thread _thread = null;

        private IWorkspaceViewModel _workspaceViewModel;
        private IErrorListViewModel _errorListViewModel;

        private const string START_SMARUNBOOK = "Start-SmaRunbook";

        public ParserService()
        {

        }

        public void Start()
        {
            _workspaceViewModel = Core.Resolve<IWorkspaceViewModel>();
            _errorListViewModel = Core.Resolve<IErrorListViewModel>();

            _thread = new Thread(new ThreadStart(delegate()
            {
                Token[] tokens;
                ParseError[] parseErrors;

                while (_running)
                {
                    string scriptContent = string.Empty;
                    IDocumentViewModel document = null;

                    // Return the active document from our workspace view model
                    document = GetDocument(out scriptContent);

                    // document will be null if the document is a credential or variable
                    if (document == null)
                        continue;

                    // Run the script through the PS parsers
                    Parser.ParseInput(scriptContent, out tokens, out parseErrors);

                    if (parseErrors.Length > 0)
                    {
                        HandleParseErrors(document, parseErrors);
                    }
                    else if (parseErrors.Length == 0 && document.Icon.Equals(Icons.ParseError))
                    {
                        ClearParseErrors(document);
                    }

                    // Try to find all commands in the script, this is done in order
                    // to find any references between runbooks
                    /*var commandTokens = tokens.Where(t => t.TokenFlags.Equals(TokenFlags.CommandName)).ToList();

                    if (commandTokens.Count > 0)
                    {
                        HandleCommandTokens(document, tokens, commandTokens);
                    }*/

                    // Clean up
                    parseErrors = null;
                    tokens = null;

                    // Sleep for 2 seconds
                    Thread.Sleep(2 * 1000);
                }
            }));

            _thread.IsBackground = true;
            _thread.Start();
        }

        public void ParseCommandTokens(IDocumentViewModel document)
        {
            Token[] tokens;
            ParseError[] parseErrors;

            if (_workspaceViewModel == null || _errorListViewModel == null)
            {
                _workspaceViewModel = Core.Resolve<IWorkspaceViewModel>();
                _errorListViewModel = Core.Resolve<IErrorListViewModel>();
            }

            Parser.ParseInput(document.Content, out tokens, out parseErrors);

            // Try to find all commands in the script, this is done in order
            // to find any references between runbooks
            var commandTokens = tokens.Where(t => t.TokenFlags.Equals(TokenFlags.CommandName)).ToList();

            if (commandTokens.Count > 0)
            {
                HandleCommandTokens(document, tokens, commandTokens);
            }
        }

        private IDocumentViewModel GetDocument(out string scriptContent)
        {
            IDocumentViewModel document = null;
            scriptContent = string.Empty;

            lock (_sync)
            {
                document = _workspaceViewModel.CurrentDocument;
                if (document == null)
                {
                    Thread.Sleep(2 * 1000);
                    return null;
                }

                if (!(document is RunbookViewModel))
                {
                    Thread.Sleep(2 * 1000);
                    return null;
                }

                if ((DateTime.Now - document.LastTimeKeyDown) < TimeSpan.FromSeconds(1))
                {
                    Thread.Sleep(2 * 1000);
                    return null;
                }

                scriptContent = document.Content;
            }

            return document;
        }

        private void HandleCommandTokens(IDocumentViewModel document, Token[] rawTokens, List<Token> tokens)
        {
            if (App.Current == null)
                return;

            App.Current.Dispatcher.Invoke(delegate()
            {
                document.References.Clear();
            });

            foreach (var token in tokens)
            {
                if (!token.Text.Equals(START_SMARUNBOOK, StringComparison.InvariantCultureIgnoreCase))
                    continue;

                var tokenIndex = rawTokens.ToList().IndexOf(token);
                var parameterName = ParseRunbookName(tokenIndex, rawTokens);

                if (string.IsNullOrEmpty(parameterName))
                    continue;

                var reference = new DocumentReference
                {
                    Source = ((RunbookViewModel)document).RunbookName,
                    Destination = parameterName,
                    LineNumber = token.Extent.StartLineNumber
                };

                if (App.Current == null)
                    break;

                App.Current.Dispatcher.Invoke(delegate()
                {
                    document.References.Add(reference);
                });
            }
        }

        private string ParseRunbookName(int tokenIndex, Token[] tokens)
        {
            for (int i = tokenIndex; i < tokens.Length; i++)
            {
                if (tokens[i].Kind == TokenKind.Parameter && tokens[i].Text.Equals("-name", StringComparison.InvariantCultureIgnoreCase) && !tokens[i].HasError)
                {
                    if (tokens.Length <= (i + 1))
                        return "";

                    if (tokens.Length <= (i + 2))
                        return "";
                    
                    return tokens[i + 1].Text.Replace("\"", "").Trim();
                }
                else if ((tokens[i].Kind == TokenKind.Identifier ||
                    tokens[i].Kind == TokenKind.StringExpandable ||
                    tokens[i].Kind == TokenKind.StringLiteral) &&
                    !tokens[i].HasError)
                {
                    return tokens[i].Text.Replace("\"", "").Trim();
                }
            }

            return "";
        }

        private void HandleParseErrors(IDocumentViewModel document, ParseError[] parseErrors)
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

        private void ClearParseErrors(IDocumentViewModel document)
        {
            document.Icon = Icons.Runbook;

            _errorListViewModel.RemoveErrorByRunbook(((RunbookViewModel)document).RunbookName);
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
