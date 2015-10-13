using SMAStudio.ViewModels;
using SMAStudio.Util;
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

        private IComponentsViewModel _componentsViewModel;
        private IWorkspaceViewModel _workspaceViewModel;
        private IErrorListViewModel _errorListViewModel;

        private const string START_SMARUNBOOK = "Start-SmaRunbook";

        public ParserService()
        {

        }

        /// <summary>
        /// Starts the parser service running in a separate thread, scanning
        /// through it looking for runbook references or parse errors
        /// </summary>
        public void Start()
        {
            _workspaceViewModel = Core.Resolve<IWorkspaceViewModel>();
            _errorListViewModel = Core.Resolve<IErrorListViewModel>();
            _componentsViewModel = Core.Resolve<IComponentsViewModel>();

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
                    
                    // Clean up
                    parseErrors = null;
                    tokens = null;

                    // Sleep for 2 seconds
                    Thread.Sleep(1 * 1000);
                }
            }));

            _thread.IsBackground = true;
            _thread.Start();
        }

        /// <summary>
        /// Parses a document and tokenizes it
        /// </summary>
        /// <param name="document">Document to work with</param>
        public void ParseCommandTokens(IDocumentViewModel document)
        {
            Token[] tokens;
            ParseError[] parseErrors;

            if (_workspaceViewModel == null || _errorListViewModel == null || _componentsViewModel == null)
            {
                _workspaceViewModel = Core.Resolve<IWorkspaceViewModel>();
                _errorListViewModel = Core.Resolve<IErrorListViewModel>();
                _componentsViewModel = Core.Resolve<IComponentsViewModel>();
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

        /// <summary>
        /// Retrieves the document content of the currently active page.
        /// Takes into account whether or not we're still typing in the textbox
        /// and if that's the case, we wait until we're done before processing
        /// </summary>
        /// <param name="scriptContent">Content of the document</param>
        /// <returns>Document</returns>
        private IDocumentViewModel GetDocument(out string scriptContent)
        {
            IDocumentViewModel document = null;
            scriptContent = string.Empty;

            lock (_sync)
            {
                document = _workspaceViewModel.CurrentDocument;
                if (document == null)
                {
                    Thread.Sleep(1 * 1000);
                    return null;
                }

                if (!(document is RunbookViewModel))
                {
                    Thread.Sleep(1 * 1000);
                    return null;
                }

                if ((DateTime.Now - document.LastTimeKeyDown) < TimeSpan.FromSeconds(1))
                {
                    Thread.Sleep(1 * 1000);
                    return null;
                }

                scriptContent = document.Content;
            }

            return document;
        }

        /// <summary>
        /// Loops through all tokens and look for a call to another runbook
        /// </summary>
        /// <param name="document">Document to work on</param>
        /// <param name="rawTokens">List of tokens</param>
        /// <param name="tokens">Tokens of type CommandName</param>
        private void HandleCommandTokens(IDocumentViewModel document, Token[] rawTokens, List<Token> tokens)
        {
            if (App.Current == null)
                return;

            App.Current.Dispatcher.Invoke(delegate()
            {
                if (document is RunbookViewModel)
                    document.References.Clear();
            });

            foreach (var token in tokens)
            {
                if (!token.Text.Equals(START_SMARUNBOOK, StringComparison.InvariantCultureIgnoreCase) &&
                    token.TokenFlags != TokenFlags.CommandName)
                {
                    continue;
                }

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

        /// <summary>
        /// Parses the runbook name and returns it (if found)
        /// </summary>
        /// <param name="tokenIndex">Index in the tokenized array to start searching from</param>
        /// <param name="tokens">List of tokens</param>
        /// <returns>Name of the runbook</returns>
        private string ParseRunbookName(int tokenIndex, Token[] tokens)
        {
            for (int i = tokenIndex; i < tokens.Length; i++)
            {
                if ((tokens[i].Kind == TokenKind.Parameter
                        && tokens[i].Text.Equals("-name", StringComparison.InvariantCultureIgnoreCase)
                        && !tokens[i].HasError)
                    || (tokens[i].Kind == TokenKind.Generic
                        && tokens[i].TokenFlags == TokenFlags.CommandName)
                        && !tokens[i].Text.Equals(START_SMARUNBOOK, StringComparison.InvariantCultureIgnoreCase))
                {
                    // A runbook can be referenced by simple calling the runbook by name and passing
                    // the parameters along. This is like calling whatever cmdlet in PS. Therefore, if
                    // it's a command, we need to verify if it is a runbook or not
                    if (tokens[i].TokenFlags == TokenFlags.CommandName)
                    {
                        if (VerifyCommandIsRunbook(tokens[i].Text))
                            return tokens[i].Text;
                        else
                            return "";
                    }

                    if (tokens.Length <= (i + 1))
                        return "";

                    if (tokens.Length <= (i + 2))
                        return "";

                    // We only want to return this reference if the reference is completed.
                    // We will end up here while typing the 'Start-SmaRunbook' command and
                    // only want to list this item if the command is completely typed in.
                    if (tokens[i + 1].Text.OccurrencesOf('"') < 2)
                        return "";
                    
                    return tokens[i + 1].Text.Replace("\"", "").Trim();
                }
                else if ((tokens[i].Kind == TokenKind.Identifier ||
                    tokens[i].Kind == TokenKind.StringExpandable ||
                    tokens[i].Kind == TokenKind.StringLiteral) &&
                    !tokens[i].HasError)
                {
                    // We only want to return this reference if the reference is completed.
                    // We will end up here while typing the 'Start-SmaRunbook' command and
                    // only want to list this item if the command is completely typed in.
                    if (tokens[i].Text.OccurrencesOf('"') < 2)
                        return "";

                    return tokens[i].Text.Replace("\"", "").Trim();
                }
            }

            return "";
        }

        /// <summary>
        /// Creates a parse error in our error list
        /// </summary>
        /// <param name="document"></param>
        /// <param name="parseErrors"></param>
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

        /// <summary>
        /// Verifies that a command call is a runbook and not another command
        /// </summary>
        /// <param name="commandName">Name to check</param>
        /// <returns>True if found, false if not</returns>
        private bool VerifyCommandIsRunbook(string commandName)
        {
            var runbook = _componentsViewModel.Runbooks.Where(r => r.RunbookName.Equals(commandName, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();

            if (runbook == null)
                return false;

            return true;
        }

        /// <summary>
        /// Removes all entries by a specific runbook
        /// </summary>
        /// <param name="document"></param>
        private void ClearParseErrors(IDocumentViewModel document)
        {
            document.Icon = Icons.Runbook;

            _errorListViewModel.RemoveErrorByRunbook(((RunbookViewModel)document).RunbookName);
            _workspaceViewModel.StatusBarText = string.Empty;
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
