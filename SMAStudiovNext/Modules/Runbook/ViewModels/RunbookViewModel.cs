using Caliburn.Micro;
using Gemini.Framework;
using Gemini.Framework.Commands;
using Gemini.Framework.Services;
using Gemini.Modules.Output;
using ICSharpCode.AvalonEdit.CodeCompletion;
using SMAStudio.Language;
using SMAStudiovNext.Core;
using SMAStudiovNext.Language;
using SMAStudiovNext.Models;
using SMAStudiovNext.Modules.ExecutionResult.ViewModels;
using SMAStudiovNext.Modules.JobHistory.ViewModels;
using SMAStudiovNext.Modules.Runbook.CodeCompletion;
using SMAStudiovNext.Modules.Runbook.Commands;
using SMAStudiovNext.Modules.Runbook.Snippets;
using SMAStudiovNext.Modules.Runbook.Views;
using SMAStudiovNext.Modules.Shell.Commands;
using SMAStudiovNext.Modules.StartRunDialog.Windows;
using SMAStudiovNext.Services;
using SMAStudiovNext.SMA;
using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Linq;
using System.Management.Automation.Language;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SMAStudiovNext.Modules.Runbook.ViewModels
{
    public sealed class RunbookViewModel : Document, IViewModel, 
        ICommandHandler<TestCommandDefinition>, 
        ICommandHandler<RunCommandDefinition>, 
        ICommandHandler<SaveCommandDefinition>,
        ICommandHandler<PublishCommandDefinition>
    {
        private readonly IBackendContext _backendContext;
        private readonly ISnippetsCollection _snippetsCollection;
        private readonly IStatusManager _statusManager;
        private readonly object _syncLock = new object();

        private readonly PowershellContext _codeContext = null;
        private readonly ILocalCodeCompletionContext _completionContext;
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

            _codeContext = new PowershellContext();
            _completionContext = new CodeCompletionContext();

            _completionContext.Start();

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
                }

                if (_runbook.PublishedRunbookVersionID.HasValue)
                {
                    GetContent(RunbookType.Published, true);
                }

                Execute.OnUIThreadAsync(() =>
                {
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
            var context = _codeContext.GetContext(CaretOffset);
            ShowCompletion(CaretOffset, context, null, true);
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

        /// <summary>
        /// Fired when text has been entered in the TextEditor, this is triggered after the text
        /// is rendered in the editor and is also responsible for displaying code completion (if needed).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnTextEntered(object sender, TextCompositionEventArgs e)
        {
            var cachedCaretOffset = _view.TextEditor.CaretOffset;
            var cachedText = Content;

            AsyncExecution.Run(System.Threading.ThreadPriority.BelowNormal, delegate ()
            {
                _codeContext.Parse(cachedText);

                var context = _codeContext.GetContext(cachedCaretOffset);

                if (context[0].Type == ExpressionType.Parameter ||
                    context[0].Type == ExpressionType.Keyword ||
                    context[0].Type == ExpressionType.Variable)
                {
                    // We need to find the "whole" word before showing auto completion
                    string word = "";

                    for (int i = cachedCaretOffset - 1; i >= 0; i--)
                    {
                        var ch = cachedText[i];

                        if (ch == ' ' || ch == '\t' || ch == '\n' || ch == '\r')
                            break;

                        word = cachedText[i] + word;
                    }

                    ShowCompletion(cachedCaretOffset, context, word, false);
                }
            });
        }

        /// <summary>
        /// Gets called when code completion should be shown.
        /// </summary>
        /// <param name="cachedCaretOffset">Location of where the cursor is at the moment. This is a cached value since the control is thread sensitive.</param>
        /// <param name="contextName">Context name is the name of the current position in the document based on the parsed context from PowershellParser.</param>
        /// <param name="text">Text that is being parsed (code content).</param>
        /// <param name="controlSpace">True if ctrl+space has been pressed, otherwise false.</param>
        private void ShowCompletion(int cachedCaretOffset, List<PowershellSegment> contextList, string text, bool controlSpace)
        {
            if (_completionWindow != null)
                return;

            if (!controlSpace && text.Trim().Length == 0)
                return;

            var data = new List<ICompletionEntry>();
            
            switch (contextList[0].Type)
            {
                case ExpressionType.Variable:
                    data.AddRange(CodeCompletionContext.Variables);
                    break;
                case ExpressionType.None:
                case ExpressionType.Script:
                case ExpressionType.Keyword:
                    data.AddRange(_snippetsCollection.Snippets.Select(s => new SnippetCompletionData(s)).ToList());
                    data.AddRange(CodeCompletionContext.Keywords);
                    data.AddRange(CodeCompletionContext.GlobalKeywords);
                    data.AddRange(CodeCompletionContext.GlobalRunbooks);
                    break;
                case ExpressionType.Parameter:
                    //if (((ICodeCompletionContext)CodeCompletionContext).CurrentKeyword == null)
                    if (contextList.Count > 0)
                    {
                        // We need to find which keyword we're focused on
                        var context = contextList.FirstOrDefault(c => c.Type == ExpressionType.Keyword);
                        if (context != null)
                        {
                            // Check if it's a runbook
                            var runbookCompletion = CodeCompletionContext.GlobalRunbooks.FirstOrDefault(r => r.Name.Equals(context.Value));
                            if (runbookCompletion != null)
                            {
                                var runbook = _backendContext.Runbooks.FirstOrDefault(r => ((RunbookModelProxy)r.Tag).RunbookName.Equals(runbookCompletion.Name));

                                if (runbook != null)
                                {
                                    var viewModel = ((RunbookModelProxy)runbook.Tag).GetViewModel<RunbookViewModel>();

                                    if (viewModel != null)
                                        data.AddRange(viewModel.GetParameters((KeywordCompletionData)runbookCompletion));
                                }
                            }
                            else
                            {
                                var keyword = CodeCompletionContext.Keywords.FirstOrDefault(k => k.Name.Equals(context.Value));

                                if (keyword == null)
                                    keyword = CodeCompletionContext.GlobalKeywords.FirstOrDefault(k => k.Name.Equals(context.Value));

                                if (keyword != null)
                                    ((ICodeCompletionContext)CodeCompletionContext).CurrentKeyword = keyword;

                                data.AddRange(((ICodeCompletionContext)CodeCompletionContext).GetParameters());
                            }
                        }
                    }
                    break;
            }

            if (data.Count == 0)
                return;

            data = data.OrderBy(d => d.DisplayText).ToList();

            lock (data)
            {
                AsyncExecution.ExecuteOnUIThread(delegate ()
                {
                    _completionWindow = new CompletionWindow(_view.TextEditor.TextArea);
                    _completionWindow.CloseAutomatically = true;
                    _completionWindow.CloseWhenCaretAtBeginning = true;
                    _completionWindow.MinWidth = 260;

                    if (text != null)
                        _completionWindow.StartOffset -= text.Length;

                    if (controlSpace)
                        _completionWindow.StartOffset = cachedCaretOffset + 1;

                    foreach (var dataItem in data)
                        _completionWindow.CompletionList.CompletionData.Add((ICompletionData)dataItem);

                    if (text != null)
                        _completionWindow.CompletionList.SelectItem(text);

                    _completionWindow.Show();
                    _completionWindow.Closed += (o, args) => _completionWindow = null;
                });
            }
        }

        /// <summary>
        /// Called when the tab has been confirmed to close and is being closed, this should take care of
        /// closing any open connections, clear caches etc.
        /// </summary>
        /// <param name="close"></param>
        protected override void OnDeactivate(bool close)
        {
            // We only want the active one to contain data so that we
            // don't chew up all memory on the machine we're working on.
            if (_codeContext != null)
                _codeContext.ClearCache();

            if (close)
                _completionContext.Stop();

            base.OnDeactivate(close);
        }

        /// <summary>
        /// Retrieve content for the specified runbook from SMA
        /// </summary>
        /// <param name="runbookType"></param>
        /// <param name="forceDownload"></param>
        public void GetContent(RunbookType runbookType, bool forceDownload = false)
        {
            var output = IoC.Get<IOutput>();

            if (!forceDownload && !String.IsNullOrEmpty(_content))
                return;

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

                            //Content = _backendContext.GetContent(Uri.AbsoluteUri + "/DraftRunbookVersion/$value");
                            Content = _backendContext.GetContent(_backendContext.Service.GetBackendUrl(runbookType, _runbook));
                            _codeContext.Parse(Content);

                            stop = DateTime.Now;
                            output.AppendLine("Content fetched in " + (stop - start).TotalMilliseconds + " ms");
                            break;
                        case RunbookType.Published:
                            output.AppendLine("Fetching 'Published' of runbook.");
                            start = DateTime.Now;

                            //Content = GetContentInternal(Uri.AbsoluteUri + "/PublishedRunbookVersion");
                            //var publishedContent = _backendContext.GetContent(Uri.AbsoluteUri + "/PublishedRunbookVersion/$value");
                            var publishedContent = _backendContext.GetContent(_backendContext.Service.GetBackendUrl(runbookType, _runbook));
                            Execute.OnUIThread(() =>
                            {
                                _view.PublishedTextEditor.Text = publishedContent;
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
        }

        /// <summary>
        /// Parses the document and retrieves any parameters found in the Param( ... ) block of the code.
        /// </summary>
        /// <param name="completionData">KeywordCompletionData that the parameters should be added to.</param>
        /// <returns>List of parameters found</returns>
        public IList<ICompletionEntry> GetParameters(KeywordCompletionData completionData)
        {
            GetContent(RunbookType.Draft, false); // we need to make sure that we have the content downloaded

            Token[] tokens;
            ParseError[] parseErrors;

            if (completionData == null)
                completionData = new KeywordCompletionData("");

            var scriptBlock = System.Management.Automation.Language.Parser.ParseInput(Content, out tokens, out parseErrors);

            if ((scriptBlock.EndBlock == null || scriptBlock.EndBlock.Statements.Count == 0))
            {
                //if (!silent)
                //    MessageBox.Show("Your runbook is broken and it's possible that the runbook won't run. Please fix any errors.", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);

                return new List<ICompletionEntry>();
            }

            var functionBlock = (FunctionDefinitionAst)scriptBlock.EndBlock.Statements[0];

            if (functionBlock.Body.ParamBlock != null)
            {
                if (functionBlock.Body.ParamBlock.Parameters == null)
                {
                    return new List<ICompletionEntry>();
                }

                foreach (var param in functionBlock.Body.ParamBlock.Parameters)
                {
                    try
                    {
                        bool isMandatory = false;
                        AttributeBaseAst attrib = null;
                        attrib = param.Attributes[param.Attributes.Count - 1]; // always the last one

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
                            DisplayText = "-" + ConvertToNiceName(param.Name.Extent.Text),
                            Name = "-" + param.Name.Extent.Text.Substring(1),                  // Remove the $
                            IsArray = (attrib.TypeName.IsArray ? true : false),
                            Type = attrib.TypeName.Name,
                            IsRequired = isMandatory
                        };

                        //parameters.Add(input);
                        completionData.Parameters.Add(input);
                    }
                    catch (Exception)
                    {
                    }
                }
            }

            //completionData.Parameters = _backendContext.Service.GetParameters(this, completionData);

            return completionData.Parameters;
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
        public PowershellSegment GetCurrentContext()
        {
            var context = _codeContext.GetContext(CaretOffset);

            if (context.Count > 0)
                return context[0];

            return null;
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
                    output.AppendLine("Unable to check out the runbook.");
                }
            }
            catch (Exception ex)
            {
                output.AppendLine("There was an error while checking in the runbook, see the error below.");
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
                    // and set DraftRunbookID to Guid.Empty so that we mimic the behaviour of SMA. This is done
                    // so that if we start editing the runbook again, it will create a new draft.
                    Content = string.Empty;
                    _runbook.DraftRunbookVersionID = Guid.Empty;
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
        }

        #region ICommandHandler<TestCommandDefinition>
        void ICommandHandler<TestCommandDefinition>.Update(Command command)
        {
            command.Enabled = true;
        }

        async Task ICommandHandler<TestCommandDefinition>.Run(Command command)
        {
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

                    AsyncExecution.Run(System.Threading.ThreadPriority.Normal, () =>
                    {
                        var guid = Owner.TestRunbook(_runbook, parameters);
                        _runbook.JobID = (Guid)guid;
                    });

                    var shell = IoC.Get<IShell>();
                    shell.OpenDocument(new ExecutionResultViewModel(this));
                }
                catch (DataServiceQueryException ex)
                {
                    var output = IoC.Get<IOutput>();
                    output.AppendLine("Error when trying to test the runbook:\r\n" + ex.Message);
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
                        pair.Name = param.Name;
                        pair.Value = ((ParameterCompletionData)param).Text;

                        parameters.Add(pair);
                    }

                    var guid = Owner.StartRunbook(_runbook, parameters);
                    _runbook.JobID = (Guid)guid;

                    var shell = IoC.Get<IShell>();
                    shell.OpenDocument(new ExecutionResultViewModel(this));
                }
                catch (DataServiceQueryException ex)
                {
                    var output = IoC.Get<IOutput>();
                    output.AppendLine("Error when trying to start the runbook:\r\n" + ex.Message);
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
                Owner.Save(this);

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
            // Do we have a running job already?
            command.Enabled = true;
        }

        async Task ICommandHandler<PublishCommandDefinition>.Run(Command command)
        {
            await CheckIn();
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

        public ILocalCodeCompletionContext CodeCompletionContext
        {
            get { return _completionContext; }
        }

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

                if (_runbook.Tags.Equals(value))
                    return;

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
