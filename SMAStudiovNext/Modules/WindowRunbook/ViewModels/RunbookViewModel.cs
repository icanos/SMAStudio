using Caliburn.Micro;
using Gemini.Framework;
using Gemini.Framework.Commands;
using Gemini.Framework.Services;
using Gemini.Modules.Output;
using ICSharpCode.AvalonEdit.CodeCompletion;
using SMAStudio.Language;
using SMAStudiovNext.Core;
using SMAStudiovNext.Language;
using SMAStudiovNext.Language.Completion;
using SMAStudiovNext.Models;
using SMAStudiovNext.Modules.ExecutionResult.ViewModels;
using SMAStudiovNext.Modules.JobHistory.ViewModels;
using SMAStudiovNext.Modules.Runbook.Commands;
using SMAStudiovNext.Modules.Runbook.Views;
using SMAStudiovNext.Modules.Shell.Commands;
using SMAStudiovNext.Modules.StartRunDialog.Windows;
using SMAStudiovNext.Services;
using SMAStudiovNext.SMA;
using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Management.Automation.Language;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Linq;
using System.Threading;
using SMAStudiovNext.Language.Snippets;
using System.Diagnostics;
using ICSharpCode.AvalonEdit.Document;

namespace SMAStudiovNext.Modules.Runbook.ViewModels
{
    public sealed class RunbookViewModel : Document, IViewModel, 
        ICommandHandler<TestCommandDefinition>, 
        ICommandHandler<RunCommandDefinition>, 
        ICommandHandler<SaveCommandDefinition>,
        ICommandHandler<PublishCommandDefinition>,
        ICommandHandler<EditPublishedCommandDefinition>
    {
        private readonly IBackendContext _backendContext;
        private readonly ISnippetsCollection _snippetsCollection;
        private readonly IStatusManager _statusManager;
        private readonly object _syncLock = new object();

        private ICompletionProvider _completionProvider;
        private CompletionWindow _completionWindow = null;

        private RunbookModelProxy _runbook;
        private string _name = "(unknown)";
        private string _content = string.Empty;
        private bool _unsavedChanges = false;
        private string _cachedSnippetContent = string.Empty;

        private IRunbookView _view;

        public RunbookViewModel(RunbookModelProxy runbook)
        {
            _backendContext = runbook.Context;
            _runbook = runbook;
            _snippetsCollection = AppContext.Resolve<ISnippetsCollection>();
            _statusManager = AppContext.Resolve<IStatusManager>();
            
            Owner = runbook.Context.Service;
        }

        /// <summary>
        /// Called before closing the tab, in case of unsaved changes we display a confirmation dialog.
        /// </summary>
        /// <param name="callback"></param>
        public override void CanClose(Action<bool> callback)
        {
            if (UnsavedChanges)
            {
                var result = MessageBox.Show("There are unsaved changes in the runbook, changes will be lost. Do you want to continue?", "Unsaved changes", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                {
                    callback(false);
                    return;
                }
            }

            callback(true);
        }

        /// <summary>
        /// Called when the view is loaded, takes care of loading the content from SMA/Azure and 
        /// hooks into some event handlers in the texteditor in order to get code completion to work
        /// correctly.
        /// </summary>
        /// <param name="view"></param>
        protected override void OnViewLoaded(object view)
        {
            _view = (IRunbookView)view;

            _completionProvider = new CompletionProvider(Owner.Context, _view.TextEditor.LanguageContext);

            if (!String.IsNullOrEmpty(_cachedSnippetContent))
            {
                AddSnippet(_cachedSnippetContent);
                _cachedSnippetContent = string.Empty;
            }

            Task.Run(() =>
            {
                if (_runbook.DraftRunbookVersionID.HasValue)
                {
                    GetContent(RunbookType.Draft, false);
                    _view.TextEditor.LanguageContext.Parse(Content).ConfigureAwait(true);
                }

                if (_runbook.PublishedRunbookVersionID.HasValue)
                {
                    GetContent(RunbookType.Published, true);
                }

                Execute.OnUIThreadAsync(() =>
                {
                    //_view.TextEditor.InitializeColorizer(_completionProvider.Context);
                    _view.PublishedTextEditor.LanguageContext.Parse(_view.PublishedTextEditor.Text).ConfigureAwait(true);

                    var diff = new GitSharp.Diff(_view.TextEditor.Text, _view.PublishedTextEditor.Text);
                    DiffSectionA = diff.Sections;
                    DiffSectionB = diff.Sections;

                    NotifyOfPropertyChange(() => DiffSectionA);
                    NotifyOfPropertyChange(() => DiffSectionB);
                });
            });

            _view.TextEditor.TextChanged += delegate (object sender, EventArgs e)
            {
                bool firstTime = String.IsNullOrEmpty(_content);
                _content = _view.TextEditor.Text;

                if (!firstTime)
                    UnsavedChanges = true;
            };

            _view.TextEditor.TextArea.TextEntered += OnTextEntered;
            _view.TextEditor.TextArea.TextEntering += OnTextEntering;
            
            #region Command Bindings
            // Open auto complete
            var ctrlSpace = new RoutedCommand();
            ctrlSpace.InputGestures.Add(new KeyGesture(Key.Space, ModifierKeys.Control));
            var cb = new CommandBinding(ctrlSpace, OnCtrlSpaceCommand);

            _view.TextEditor.CommandBindings.Add(cb);

            // Save changes
            var saveGesture = new RoutedCommand();
            saveGesture.InputGestures.Add(new KeyGesture(Key.S, ModifierKeys.Control));
            cb = new CommandBinding(saveGesture, OnSaveRunbook);

            _view.TextEditor.CommandBindings.Add(cb);

            // Show history for runbook
            var historyGesture = new RoutedCommand();
            historyGesture.InputGestures.Add(new KeyGesture(Key.H, ModifierKeys.Control));
            cb = new CommandBinding(historyGesture, OnShowHistory);

            _view.TextEditor.CommandBindings.Add(cb);
            #endregion

            _statusManager.SetTimeoutText("Tips! Use Ctrl+H to view job history.", 10);
        }
        
        /// <summary>
        /// Called when the user uses ctrl+space command in the textarea
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCtrlSpaceCommand(object sender, ExecutedRoutedEventArgs e)
        {
            ShowCompletion(completionWord: "", controlSpace: true).ConfigureAwait(true);
        }

        /// <summary>
        /// Called when the user uses ctrl+s command in the textarea
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OnSaveRunbook(object sender, ExecutedRoutedEventArgs e)
        {
            await SaveRunbook(null);
        }

        /// <summary>
        /// Called when the user uses ctrl+h command in the textarea
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnShowHistory(object sender, ExecutedRoutedEventArgs e)
        {
            var shell = IoC.Get<IShell>();
            shell.OpenDocument(new JobHistoryViewModel(this));
        }

        /// <summary>
        /// Fired when text is being entered in the TextEditor, this is triggered before the text
        /// is rendered in the editor.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnTextEntering(object sender, TextCompositionEventArgs e)
        {
            if (e.Text.Length > 0 && _completionWindow != null)
            {
                if (!char.IsLetterOrDigit(e.Text[0]) && e.Text[0] != '-')
                {
                    // Whenever a non-letter is typed while the completion window is open,
                    // insert the currently selected element.
                    _completionWindow.CompletionList.RequestInsertion(e);
                }
            }
        }

        private DateTime lastKeystrokeTime = DateTime.MinValue;

        /// <summary>
        /// Fired when text has been entered in the TextEditor, this is triggered after the text
        /// is rendered in the editor and is also responsible for displaying code completion (if needed).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnTextEntered(object sender, TextCompositionEventArgs e)
        {
            //_completionTimer.Change(200, 0);
            ShowCompletionWindow(sender).ConfigureAwait(true);
        }

        private void GetCompletionOffset(out int offset)
        {
            offset = _view.TextEditor.CaretOffset;
        }

        private async Task ShowCompletionWindow(object sender)
        {
            var word = string.Empty;
            var lineStr = string.Empty;
            var content = string.Empty;
            var caretOffset = 0;
            var line = default(DocumentLine);

            AsyncExecution.ExecuteOnUIThread(() =>
            {
                line = _view.TextEditor.Document.GetLineByOffset(_view.TextEditor.CaretOffset);
                lineStr = _view.TextEditor.Document.GetText(line);

                caretOffset = _view.TextEditor.CaretOffset;
                content = _view.TextEditor.Document.Text;
            });

            for (int i = caretOffset - 1; i >= 0; i--)
            {
                var ch = content[i];

                if (ch == ' ' || ch == '\t' || ch == '\n' || ch == '\r')
                    break;

                word = content[i] + word;
            }

            if (word == string.Empty)
                return;

            await ShowCompletion(completionWord: word, controlSpace: false);
        }

        private async Task ShowCompletion(string completionWord, bool controlSpace)
        {
            if (_completionWindow == null)
            {
                int offset;
                GetCompletionOffset(out offset);

                var line = _view.TextEditor.Document.GetLineByOffset(offset);
                var lineStr = _view.TextEditor.Document.GetText(line);
                var completionChar = controlSpace ? (char?)null : _view.TextEditor.Document.GetCharAt(offset - 1);
                var results = await _completionProvider.GetCompletionData(completionWord, lineStr, line.LineNumber, offset, completionChar).ConfigureAwait(true);

                if (results.CompletionData == null)
                    return;

                AsyncExecution.ExecuteOnUIThread(() =>
                {
                    if (_completionWindow == null && results.CompletionData.Any())
                    {
                        _completionWindow = new CompletionWindow(_view.TextEditor.TextArea)
                        {
                            CloseWhenCaretAtBeginning = controlSpace
                        };

                        if (completionChar != null && char.IsLetterOrDigit(completionChar.Value))
                            _completionWindow.StartOffset -= 1;

                        var data = _completionWindow.CompletionList.CompletionData;
                        foreach (var completion in results.CompletionData)
                        {
                            data.Add(completion);
                        }

                        _completionWindow.Show();

                        _completionWindow.Closed += (o, args) =>
                        {
                            _completionWindow = null;
                        };
                    }
                });
            }
        }

        /// <summary>
        /// Retrieve content for the specified runbook from SMA
        /// </summary>
        /// <param name="runbookType"></param>
        /// <param name="forceDownload"></param>
        public string GetContent(RunbookType runbookType, bool forceDownload = false)
        {
            var output = IoC.Get<IOutput>();
            var contentToReturn = string.Empty;

            if (!forceDownload && !String.IsNullOrEmpty(_content))
                return _content;

            output.AppendLine("Downloading content of '" + _runbook.RunbookName + "'...");

            try
            {
                DateTime start;
                DateTime stop;

                lock(_syncLock)
                {
                    switch (runbookType)
                    {
                        case RunbookType.Draft:
                            output.AppendLine("Fetching 'Draft' of runbook.");
                            start = DateTime.Now;

                            Content = _backendContext.GetContent(_backendContext.Service.GetBackendUrl(runbookType, _runbook));
                            contentToReturn = Content;

                            stop = DateTime.Now;
                            output.AppendLine("Content fetched in " + (stop - start).TotalMilliseconds + " ms");
                            break;
                        case RunbookType.Published:
                            output.AppendLine("Fetching 'Published' of runbook.");
                            start = DateTime.Now;

                            var publishedContent = _backendContext.GetContent(_backendContext.Service.GetBackendUrl(runbookType, _runbook));
                            Execute.OnUIThread(() =>
                            {
                                if (_view != null)
                                    _view.PublishedTextEditor.Text = publishedContent;

                                contentToReturn = publishedContent;
                            });

                            stop = DateTime.Now;
                            output.AppendLine("Content fetched in " + (stop - start).TotalMilliseconds + " ms");
                            break;
                    }
                }
            }
            catch (WebException e)
            {
                output.AppendLine("Unable to download runbook from SMA, error: " + e.Message);

                try
                {
                    if (e.Status != WebExceptionStatus.ConnectFailure &&
                        e.Status != WebExceptionStatus.ConnectionClosed)
                    {
                        GetContent(runbookType == RunbookType.Draft ? RunbookType.Published : RunbookType.Draft, forceDownload);
                    }
                }
                catch (WebException ex)
                {
                    output.AppendLine("Unable to retrieve any content for the runbook. Error: " + ex.Message);
                }
            }

            return contentToReturn;
        }

        /// <summary>
        /// Triggers a parse of the runbook
        /// </summary>
        public void ParseContent()
        {
            if (_view == null)
                return;

            var contentToParse = string.Empty;
            var context = default(LanguageContext);

            Execute.OnUIThread(() =>
            {
                contentToParse = _view.TextEditor.Text;
                context = _view.TextEditor.LanguageContext;
            });

            Task.Run(async () =>
            {
                await context.Parse(contentToParse);

                Execute.OnUIThread(() =>
                {
                    _view.TextEditor.InvalidateVisual();
                });
            });
        }

        /// <summary>
        /// Parses the document and retrieves any parameters found in the Param( ... ) block of the code.
        /// </summary>
        /// <param name="completionWord">Word that the parameter needs to start with</param>
        /// <returns>List of parameters found</returns>
        public IList<ICompletionData> GetParameters(string completionWord)//(KeywordCompletionData completionData)
        {
            var completionEntries = new List<ICompletionData>();
            var fixedCompletionWord = completionWord.Replace("-", "");
            Token[] tokens;
            ParseError[] parseErrors;

            //if (completionData == null)
            //    completionData = new KeywordCompletionData("");
            string contentToParse = Content;
            if (String.IsNullOrEmpty(Content))
            {
                GetContent(RunbookType.Draft, true);
                contentToParse = Content;

                if (String.IsNullOrEmpty(Content))
                {
                    GetContent(RunbookType.Published, true);
                    contentToParse = _view.PublishedTextEditor.Text;
                }
            }
            
            var scriptBlock = System.Management.Automation.Language.Parser.ParseInput(contentToParse, out tokens, out parseErrors);

            if ((scriptBlock.EndBlock == null || scriptBlock.EndBlock.Statements.Count == 0))
            {
                //if (!silent)
                //    MessageBox.Show("Your runbook is broken and it's possible that the runbook won't run. Please fix any errors.", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);

                return new List<ICompletionData>();
            }

            var functionBlock = (FunctionDefinitionAst)scriptBlock.EndBlock.Statements[0];

            if (functionBlock.Body.ParamBlock != null)
            {
                if (functionBlock.Body.ParamBlock.Parameters == null)
                {
                    return new List<ICompletionData>();
                }

                foreach (var param in functionBlock.Body.ParamBlock.Parameters)
                {
                    try
                    {
                        bool isMandatory = false;
                        AttributeBaseAst attrib = null;
                        attrib = param.Attributes[param.Attributes.Count - 1]; // always the last one

                        if (!param.Name.Extent.Text.Substring(1).StartsWith(fixedCompletionWord, StringComparison.InvariantCultureIgnoreCase))
                            continue;

                        if (param.Attributes.Count > 1)
                        {
                            // Probably contains a Parameter(Mandatory = ...) or something, check it out
                            if (param.Attributes[0] is AttributeAst)
                            {
                                var parameterAttrib = (AttributeAst)param.Attributes[0];
                                if (parameterAttrib.Extent.Text.Contains("[Parameter") ||
                                    parameterAttrib.Extent.Text.Contains("[parameter"))
                                {
                                    foreach (var namedParameter in parameterAttrib.NamedArguments)
                                    {
                                        if (namedParameter.ArgumentName.Equals("Mandatory", StringComparison.InvariantCultureIgnoreCase))
                                        {
                                            isMandatory = namedParameter.Argument.Extent.Text.Equals("$true") ? true : false;
                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        var input = new ParameterCompletionData
                        {
                            RawName = param.Name.Extent.Text.Substring(1),
                            DisplayText = "-" + ConvertToNiceName(param.Name.Extent.Text) + (isMandatory ? " (required)" : ""),
                            Name = "-" + param.Name.Extent.Text.Substring(1),                  // Remove the $
                            IsArray = (attrib.TypeName.IsArray ? true : false),
                            Type = attrib.TypeName.Name,
                            IsRequired = isMandatory
                        };

                        //parameters.Add(input);
                        completionEntries.Add(input);
                    }
                    catch (Exception)
                    {
                    }
                }
            }

            //completionData.Parameters = _backendContext.Service.GetParameters(this, completionData);

            return completionEntries;
        }

        /// <summary>
        /// Converts the parameter to a nice name, removing the $ at the start and converts
        /// the first char to upper case.
        /// </summary>
        /// <param name="parameterName"></param>
        /// <returns></returns>
        private string ConvertToNiceName(string parameterName)
        {
            if (parameterName == null)
                return string.Empty;

            if (parameterName.Length == 0)
                return string.Empty;

            parameterName = parameterName.Replace("$", "");
            parameterName = char.ToUpper(parameterName[0]) + parameterName.Substring(1);

            return parameterName;
        }

        /// <summary>
        /// Returns the context in which the caret currenlty is located.
        /// </summary>
        /// <returns></returns>
        public LanguageSegment GetCurrentContext()
        {
            //var context = _codeContext.GetContext(CaretOffset);
            return _completionProvider.Context.GetCurrentContext(CaretOffset);
        }

        /// <summary>
        /// Takes the Published version of the runbook and copies it into a new Draft.
        /// </summary>
        /// <returns>Async task</returns>
        public async Task CheckOut()
        {
            var output = IoC.Get<IOutput>();

            try {
                var result = await Owner.CheckOut(this);
                if (!result)
                {
                    output.AppendLine("Unable to edit the runbook.");
                }
            }
            catch (Exception ex)
            {
                output.AppendLine("There was an error while editing the runbook, see the error below.");
                output.AppendLine(ex.Message);
            }
        }

        /// <summary>
        /// Publishes a draft runbook.
        /// </summary>
        /// <returns>Async task</returns>
        public async Task CheckIn()
        {
            var output = IoC.Get<IOutput>();

            try {
                await SaveRunbook(null);

                var result = await Owner.CheckIn(_runbook);
                if (!result)
                {
                    output.AppendLine("Unable to check in the runbook.");
                }
                else
                {
                    CommandManager.InvalidateRequerySuggested();
                    output.AppendLine("The runbook has been published.");

                    // Since when publishing a runbook, it removes the draft, we have to clear the editor
                    // and set DraftRunbookID to null so that we mimic the behaviour of SMA. This is done
                    // so that if we start editing the runbook again, it will create a new draft.
                    Content = string.Empty;
                    _runbook.DraftRunbookVersionID = null;

                    // Download the newly published runbook
                    GetContent(RunbookType.Published, true);

                    UnsavedChanges = false;
                }
            }
            catch (Exception ex)
            {
                output.AppendLine("There was an error while publishing the runbook, see the error below.");
                output.AppendLine(ex.Message);
            }
        }

        /// <summary>
        /// Checks for running jobs for the current runbook.
        /// </summary>
        /// <param name="checkDraft">True if draft is being checked, otherwise will check if the published version is running.</param>
        /// <returns>True for draft, false for published</returns>
        public async Task<bool> CheckRunningJobs(bool checkDraft)
        {
            return await Owner.CheckRunningJobs(_runbook, checkDraft);
        }

        /// <summary>
        /// Adds a snippet to the code editor (eg. the template content)
        /// </summary>
        /// <param name="snippetContent">Snippet content to add</param>
        public void AddSnippet(string snippetContent)
        {
            if (_view == null)
            {
                // The view hasn't been loaded yet, which means that we can't add
                // the snippet right now. Because of this, we need to cache the snippet
                // and insert it when the view has been successfully loaded instead.
                _cachedSnippetContent = snippetContent;
                return;
            }

            var codeSnippet = new CodeSnippet();
            codeSnippet.Text = snippetContent;

            var snippet = codeSnippet.CreateAvalonEditSnippet(_runbook);
            snippet.Insert(_view.TextEditor.TextArea);

            UnsavedChanges = true;
        }

        #region ICommandHandler<TestCommandDefinition>
        void ICommandHandler<TestCommandDefinition>.Update(Command command)
        {
            command.Enabled = true;
        }

        async Task ICommandHandler<TestCommandDefinition>.Run(Command command)
        {
            var output = IoC.Get<IOutput>();
            var dialog = new PrepareRunWindow(this);

            if ((bool)dialog.ShowDialog())
            {
                // We should start the runbook
                if (UnsavedChanges)
                    await SaveRunbook(null);

                if (await CheckRunningJobs(true))
                {
                    MessageBox.Show("There is currently a running job, please wait for it to finish.", "Running Jobs", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                try
                {
                    // Convert to NameValuePair
                    var parameters = new List<NameValuePair>();

                    foreach (var param in dialog.Inputs)
                    {
                        var pair = new NameValuePair();
                        pair.Name = (param as ParameterCompletionData).RawName;
                        pair.Value = ((ParameterCompletionData)param).Value;

                        parameters.Add(pair);
                    }

                    var executionViewModel = new ExecutionResultViewModel(this);
                    var shell = IoC.Get<IShell>();
                    shell.OpenDocument(executionViewModel);

                    Execute.OnUIThread(() =>
                    {
                        output.AppendLine("Starting a test of '" + _runbook.RunbookName + "'...");
                    });

                    AsyncExecution.Run(System.Threading.ThreadPriority.Normal, () =>
                    {
                        var guid = Owner.TestRunbook(_runbook, parameters);

                        if (guid.HasValue)
                        {
                            _runbook.JobID = (Guid)guid;
                        }
                        else
                        {
                            Execute.OnUIThread(() =>
                            {
                                shell.CloseDocument(executionViewModel);
                            });
                        }
                    });
                }
                catch (DataServiceQueryException ex)
                {
                    Execute.OnUIThread(() =>
                    {
                        output.AppendLine("Error when trying to test the runbook:\r\n" + ex.Message);
                    });
                }
            }
        }
        #endregion

        #region ICommandHandler<RunCommandDefinition>
        void ICommandHandler<RunCommandDefinition>.Update(Command command)
        {
            // Do we have a running job already?
            if (_runbook.PublishedRunbookVersionID.HasValue)
                command.Enabled = true;
            else
                command.Enabled = false;
        }

        async Task ICommandHandler<RunCommandDefinition>.Run(Command command)
        {
            var output = IoC.Get<IOutput>();
            var dialog = new PrepareRunWindow(this);

            if ((bool)dialog.ShowDialog())
            {
                // We should start the runbook
                if (UnsavedChanges)
                    await SaveRunbook(null);

                if (await CheckRunningJobs(false))
                {
                    MessageBox.Show("There is currently a running job, please wait for it to finish.", "Running Jobs", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                try
                {
                    // Convert to NameValuePair
                    var parameters = new List<NameValuePair>();

                    foreach (var param in dialog.Inputs)
                    {
                        var pair = new NameValuePair();
                        pair.Name = (param as ParameterCompletionData).RawName;
                        pair.Value = ((ParameterCompletionData)param).Value;

                        parameters.Add(pair);
                    }

                    var executionViewModel = new ExecutionResultViewModel(this);
                    var shell = IoC.Get<IShell>();
                    shell.OpenDocument(executionViewModel);

                    Execute.OnUIThread(() => { output.AppendLine("Starting a run of the published version of '" + _runbook.RunbookName + "'..."); });

                    AsyncExecution.Run(System.Threading.ThreadPriority.Normal, () =>
                    {
                        var guid = Owner.StartRunbook(_runbook, parameters);

                        if (guid.HasValue)
                        {
                            _runbook.JobID = (Guid)guid;
                        }
                        else
                        {
                            Execute.OnUIThread(() =>
                            {
                                shell.CloseDocument(executionViewModel);
                            });
                        }
                    });
                }
                catch (DataServiceQueryException ex)
                {
                    Execute.OnUIThread(() =>
                    {
                        output.AppendLine("Error when trying to run the runbook:\r\n" + ex.Message);
                    });
                }
            }
        }
        #endregion

        #region ICommandHandler<SaveCommandDefinition>
        void ICommandHandler<SaveCommandDefinition>.Update(Command command)
        {
            // Do we have a running job already?
            command.Enabled = true;
        }

        async Task ICommandHandler<SaveCommandDefinition>.Run(Command command)
        {
            command.Enabled = false;

            await SaveRunbook(command);
        }

        private async Task SaveRunbook(Command command)
        {
            await Task.Run(delegate () {
                //try
                //{
                    Owner.Save(this);
                //}
                //catch (PersistenceException ex)
               // {
                //    MessageBox.Show("Error: " + ex.Message, "Unable to save", MessageBoxButton.OK, MessageBoxImage.Error);
                //}

                _runbook.ViewModel = this;

                //var backendContext = AppContext.Resolve<IBackendContext>();
                _backendContext.AddToRunbooks(_runbook);

                // Update the UI to notify that the changes has been saved
                UnsavedChanges = false;

                if (command != null)
                    command.Enabled = true;
            });
        }
        #endregion

        #region ICommandHandler<CheckInCommandDefinition>
        void ICommandHandler<PublishCommandDefinition>.Update(Command command)
        {
            /*if (_runbook.DraftRunbookVersionID.HasValue)
                command.Enabled = true;
            else
                command.Enabled = false;*/
            if (!_view.PublishedTextEditor.Text.Equals(_view.TextEditor.Text))
                command.Enabled = true;
            else
                command.Enabled = false;
        }

        async Task ICommandHandler<PublishCommandDefinition>.Run(Command command)
        {
            await CheckIn();
        }

        void ICommandHandler<EditPublishedCommandDefinition>.Update(Command command)
        {
            if (_runbook.PublishedRunbookVersionID.HasValue)
                command.Enabled = true;
            else
                command.Enabled = false;
        }

        async Task ICommandHandler<EditPublishedCommandDefinition>.Run(Command command)
        {
            await CheckOut();
        }
        #endregion

        #region Properties
        public IBackendService Owner
        {
            private get;
            set;
        }

        public ICommand GoToDefinitionCommand
        {
            get { return AppContext.Resolve<ICommand>("GoToDefinitionCommand"); }
        }

        public RunbookModelProxy Runbook
        {
            get { return _runbook; }
        }

        /*public ILocalCodeCompletionContext CodeCompletionContext
        {
            get { return _completionContext; }
        }*/

        public int CaretOffset
        {
            get
            {
                return _view.TextEditor.Dispatcher.Invoke(delegate()
                {
                    if (_view == null || _view.TextEditor == null)
                        return 0;

                    return _view.TextEditor.CaretOffset;
                });
            }
        }

        public object Model
        {
            get { return Runbook; }
            set
            {
                _runbook = (RunbookModelProxy)value;
            }
        }

        public override string DisplayName
        {
            get
            {
                string displayName = _name;
                if (!String.IsNullOrEmpty(_runbook.RunbookName))
                    displayName = _runbook.RunbookName;

                if (UnsavedChanges)
                    displayName += "*";

                return displayName;
            }
            set
            {
                if (_runbook != null)
                    _runbook.RunbookName = value;
                else
                    _name = value;
            }
        }

        public string Content
        {
            get
            {
                if (_view != null)
                {
                    return _view.TextEditor.Dispatcher.Invoke(delegate ()
                    {
                        if (String.IsNullOrEmpty(_view.TextEditor.Text) && !String.IsNullOrEmpty(_content))
                            _view.TextEditor.Text = _content;

                        return _view.TextEditor.Text;
                    });
                }

                return _content;
            }
            set
            {
                if (_view != null && _view.TextEditor != null)
                {
                    _view.TextEditor.Dispatcher.Invoke(delegate ()
                    {
                        if (!_view.TextEditor.Text.Equals(value))
                        {
                            if (!String.IsNullOrEmpty(_view.TextEditor.Text))
                                UnsavedChanges = true;

                            _view.TextEditor.Text = value;
                            _content = value;

                            _view.TextEditor.InvalidateVisual();
                        }
                    });
                }
                else
                {
                    _content = value;
                }
            }
        }

        public string Tags
        {
            get { return _runbook != null ? _runbook.Tags : ""; }
            set
            {
                if (_runbook == null)
                    return;

                //if (_runbook.Tags.Equals(value))
                //    return;

                _runbook.Tags = value;
                UnsavedChanges = true;
            }
        }

        public bool UnsavedChanges
        {
            get { return _unsavedChanges; }
            set { _unsavedChanges = value; NotifyOfPropertyChange(() => DisplayName); }
        }
        /*
        public Uri Uri
        {
            get { return new Uri(_backendContext.ConnectionUrl + "/Runbooks(guid'" + _runbook.RunbookID + "')"); }
        }
        */
        public IEnumerable<GitSharp.Diff.Section> DiffSectionA { get; set; }

        public IEnumerable<GitSharp.Diff.Section> DiffSectionB { get; set; }
        #endregion
    }
}
