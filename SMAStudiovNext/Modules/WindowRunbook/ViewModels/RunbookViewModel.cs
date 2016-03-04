using Caliburn.Micro;
using Gemini.Framework;
using Gemini.Framework.Commands;
using Gemini.Framework.Services;
using Gemini.Modules.Output;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using SMAStudio.Modules.Runbook.Editor.Parser;
using SMAStudiovNext.Core;
using SMAStudiovNext.Language;
using SMAStudiovNext.Language.Snippets;
using SMAStudiovNext.Models;
using SMAStudiovNext.Modules.ExecutionResult.ViewModels;
using SMAStudiovNext.Modules.JobHistory.ViewModels;
using SMAStudiovNext.Modules.Runbook.Commands;
using SMAStudiovNext.Modules.Runbook.Editor;
using SMAStudiovNext.Modules.Runbook.Editor.Completion;
using SMAStudiovNext.Modules.Runbook.Views;
using SMAStudiovNext.Modules.Shell.Commands;
using SMAStudiovNext.Modules.StartRunDialog.Windows;
using SMAStudiovNext.Services;
using SMAStudiovNext.SMA;
using SMAStudiovNext.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Language;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SMAStudiovNext.Modules.Runbook.ViewModels
{
    public class RunbookViewModel : Document, IViewModel,
        ICommandHandler<SaveCommandDefinition>,
        ICommandHandler<PublishCommandDefinition>,
        ICommandHandler<EditPublishedCommandDefinition>,
        ICommandHandler<TestCommandDefinition>,
        ICommandHandler<RunCommandDefinition>
    {
        private readonly IBackendContext _backendContext;
        private readonly IStatusManager _statusManager;
        private readonly object _lock = new object();

        private ICompletionProvider _completionProvider;
        private CompletionWindow _completionWindow = null;
        private RunbookModelProxy _runbook;
        private IRunbookView _view;
        private bool _inTestRun = false;
        private bool _inRun = false;
        /// <summary>
        /// This variable is used mainly when creating a new runbook and
        /// a snippet is added to the runbook (default content). This may be
        /// done before the view has loaded, and therefore we can't apply it to the property
        /// Content since that would be null.
        /// </summary>
        private string _cachedDraftContent = string.Empty;

        public RunbookViewModel(RunbookModelProxy runbook)
        {
            _runbook = runbook;
            _backendContext = runbook.Context;
            _statusManager = AppContext.Resolve<IStatusManager>();

            //Owner = runbook.Context.Service;
        }

        /// <summary>
        /// Add a snippet to the draft content of the runbook
        /// </summary>
        /// <param name="content">Content to add</param>
        public void AddSnippet(string content)
        {
            if (_view == null)
            {
                _cachedDraftContent = content;
                return;
            }

            var codeSnippet = new CodeSnippet();
            codeSnippet.Text = content;

            var snippet = codeSnippet.CreateAvalonEditSnippet(_runbook);

            Execute.OnUIThread(() =>
            {
                snippet.Insert(_view.TextEditor.TextArea);
                UnsavedChanges = true;
            });
        }

        /// <summary>
        /// Get draft or published content from our backend service or cache
        /// </summary>
        /// <param name="runbookType">Type of content to retrieve</param>
        /// <param name="forceDownload">Set to true to force download of content from backend, regardless of what we have locally</param>
        /// <returns>Content of the runbook</returns>
        public string GetContent(RunbookType runbookType, bool forceDownload = false)
        {
            if (runbookType == RunbookType.Draft)
                return AsyncHelper.RunSync<string>(() => GetContentInternal(_view.TextEditor, runbookType, forceDownload));
            else
                return AsyncHelper.RunSync<string>(() => GetContentInternal(_view.PublishedTextEditor, runbookType, forceDownload));
        }

        /// <summary>
        /// Get content from our backend service or cache
        /// </summary>
        /// <param name="forceDownload">Set to true to force download of content from backend</param>
        /// <returns>Draft content</returns>
        private async Task<string> GetContentInternal(RunbookEditor editor, RunbookType runbookType, bool forceDownload)
        {
            var content = string.Empty;
            var currentContent = string.Empty;

            // Get current content
            Execute.OnUIThread(() => { currentContent = editor.Text; });

            if (forceDownload || String.IsNullOrEmpty(currentContent))
            {
                content = await _backendContext.GetContentAsync(_backendContext.Service.GetBackendUrl(runbookType, _runbook));

                Execute.OnUIThread(() =>
                {
                    lock (_lock)
                    {
                        editor.Text = content;
                    }
                });
            }
            else
            {
                Execute.OnUIThread(() =>
                {
                    lock (_lock)
                    {
                        content = editor.Text;
                    }
                });
            }

            return content;
        }
        
        /// <summary>
        /// Gets called when the view is loaded
        /// </summary>
        /// <param name="view"></param>
        protected override void OnViewLoaded(object view)
        {
            _view = (IRunbookView)view;
            _completionProvider = new CompletionProvider(_backendContext, _view.TextEditor.LanguageContext);

            if (_runbook.RunbookID != Guid.Empty)
            {
                Task.Run(() =>
                {
                    GetContent(RunbookType.Draft);
                    GetContent(RunbookType.Published);

                    var draftContent = string.Empty;// _view.TextEditor.Text;
                    var publishedContent = string.Empty;// _view.PublishedTextEditor.Text;

                    Execute.OnUIThread(() =>
                    {
                        draftContent = _view.TextEditor.Text;
                        publishedContent = _view.PublishedTextEditor.Text;
                    });

                    var diff = new GitSharp.Diff(draftContent, publishedContent);
                    DiffSectionA = diff.Sections;
                    DiffSectionB = diff.Sections;

                    NotifyOfPropertyChange(() => DiffSectionA);
                    NotifyOfPropertyChange(() => DiffSectionB);
                });
            }
            else
            {
                Execute.OnUIThread(() => { _view.TextEditor.Text = _cachedDraftContent; });
                _cachedDraftContent = string.Empty;
            }

            _view.TextEditor.TextChanged += delegate (object sender, EventArgs e)
            {
                UnsavedChanges = true;
            };

            _view.TextEditor.TextArea.TextEntering += delegate (object sender, TextCompositionEventArgs e)
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
            };

            _view.TextEditor.TextArea.TextEntered += delegate (object sender, TextCompositionEventArgs e)
            {
                ShowCompletionWindow(sender).ConfigureAwait(false);
            };

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
            ShowCompletion(completionWord: "", controlSpace: true).ConfigureAwait(false);
        }

        /// <summary>
        /// Called when the user uses ctrl+s command in the textarea
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OnSaveRunbook(object sender, ExecutedRoutedEventArgs e)
        {
            await Save(null);
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

        public LanguageSegment GetCurrentContext()
        {
            int caretOffset = 0;

            if (_view == null)
                return null;

            Execute.OnUIThread(() => { caretOffset = _view.TextEditor.CaretOffset; });

            return _completionProvider.Context.GetCurrentContext(caretOffset);
        }

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

            if (context == null)
                return;

            context.Parse(contentToParse);
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

            //AsyncExecution.ExecuteOnUIThread(() =>
            Execute.OnUIThread(() =>
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

                Execute.OnUIThread(() =>
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
        /// TODO: Async this!
        /// </summary>
        /// <param name="completionWord"></param>
        /// <returns></returns>
        public IList<ICompletionData> GetParameters(string completionWord)
        {
            var completionEntries = new List<ICompletionData>();
            var fixedCompletionWord = completionWord != null ? completionWord.Replace("-", "") : null;
            Token[] tokens;
            ParseError[] parseErrors;

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

                        if (fixedCompletionWord != null && !param.Name.Extent.Text.Substring(1).StartsWith(fixedCompletionWord, StringComparison.InvariantCultureIgnoreCase))
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
                        
                        completionEntries.Add(input);
                    }
                    catch (Exception ex)
                    {
                        var output = IoC.Get<IOutput>();
                        output.AppendLine("Error parsing parameters: " + ex);
                    }
                }
            }
            
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

        public async Task CheckIn()
        {
            var output = IoC.Get<IOutput>();

            if (UnsavedChanges)
            {
                bool saveChanges = false;

                Execute.OnUIThread(() =>
                {
                    var result = MessageBox.Show("You have unsaved changes, do you want to save them to continue?", "Unsaved changes", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                        saveChanges = true;
                });
                
                if (saveChanges)
                    await Save(null).ConfigureAwait(false);
            }

            var checkInResult = await _backendContext.Service.CheckIn(_runbook).ConfigureAwait(false);

            if (checkInResult)
            {
                CommandManager.InvalidateRequerySuggested();
                output.AppendLine("The runbook has been published.");

                Execute.OnUIThread(() =>
                {
                    _view.PublishedTextEditor.Text = _view.TextEditor.Text;
                    _view.TextEditor.Text = string.Empty;
                });

                _runbook.DraftRunbookVersionID = Guid.Empty;
            }
            else
                output.AppendLine("Unable to check in the runbook.");
        }

        public async Task CheckOut()
        {
            await _backendContext.Service.CheckOut(this).ConfigureAwait(false);
        }

        private async Task Save(Command command)
        {
            // Update the UI to notify that the changes has been saved
            Execute.OnUIThread(() => { UnsavedChanges = false; });

            var result = await _backendContext.Service.Save(this, command).ConfigureAwait(false);

            _runbook.ViewModel = this;
            _backendContext.AddToRunbooks(_runbook);

            if (command != null)
                command.Enabled = true;
        }

        void ICommandHandler<SaveCommandDefinition>.Update(Command command)
        {
            if (UnsavedChanges)
                command.Enabled = true;
            else
                command.Enabled = false;
        }

        async Task ICommandHandler<SaveCommandDefinition>.Run(Command command)
        {
            await Save(command);
        }

        void ICommandHandler<PublishCommandDefinition>.Update(Command command)
        {
            Execute.OnUIThread(() =>
            {
                if (_view.PublishedTextEditor.Text.Equals(_view.TextEditor.Text))
                    command.Enabled = false;
                else
                    command.Enabled = true;
            });
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

        void ICommandHandler<TestCommandDefinition>.Update(Command command)
        {
            if (_runbook.DraftRunbookVersionID.HasValue && !_inTestRun)
                command.Enabled = true;
            else
                command.Enabled = false;
        }

        async Task ICommandHandler<TestCommandDefinition>.Run(Command command)
        {
            _inTestRun = true;
            command.Enabled = false;

            await StartRunAsync(command, true);

            command.Enabled = true;
            _inTestRun = false;
        }

        void ICommandHandler<RunCommandDefinition>.Update(Command command)
        {
            if (_runbook.PublishedRunbookVersionID.HasValue)
                command.Enabled = true;
            else
                command.Enabled = false;
        }

        async Task ICommandHandler<RunCommandDefinition>.Run(Command command)
        {
            _inRun = true;
            command.Enabled = false;

            await StartRunAsync(command, false);

            command.Enabled = true;
            _inRun = false;
        }

        private async Task StartRunAsync(Command command, bool isDraft)
        {
            var dialog = new PrepareRunWindow(this);
            var result = (bool)dialog.ShowDialog();

            if (!result)
                return;

            if (UnsavedChanges)
                await Save(command);

            var runningJob = await _backendContext.Service.CheckRunningJobs(_runbook, isDraft).ConfigureAwait(false);
            if (runningJob)
            {
                MessageBox.Show("There is currently a running job, please wait for it to finish.", "Running Jobs", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Convert to NameValuePair
            var parameters = new List<NameValuePair>();

            foreach (var param in dialog.Inputs)
            {
                var pair = new NameValuePair();
                pair.Name = (param as ParameterCompletionData).RawName;
                pair.Value = ((ParameterCompletionData)param).Value;

                parameters.Add(pair);
            }

            // Open the execution result window
            var executionViewModel = new ExecutionResultViewModel(this);
            var shell = IoC.Get<IShell>();
            shell.OpenDocument(executionViewModel);

            var output = IoC.Get<IOutput>();
            Execute.OnUIThread(() => { output.AppendLine("Starting a " + (isDraft ? "test" : "run") + " of '" + _runbook.RunbookName + "'..."); });

            // Start the actual test
            try
            {
                await Task.Run(() =>
                {
                    var guid = default(Guid?);

                    if (isDraft)
                        guid = _backendContext.Service.TestRunbook(_runbook, parameters);
                    else
                        guid = _backendContext.Service.StartRunbook(_runbook, parameters);

                    if (guid.HasValue)
                        _runbook.JobID = guid.Value;
                });

                if (_runbook.JobID == Guid.Empty)
                    Execute.OnUIThread(() => { shell.CloseDocument(executionViewModel); });
            }
            catch (Exception ex)
            {
                Execute.OnUIThread(() => { output.AppendLine("Error when trying to " + (isDraft ? "test" : "run") + " the runbook:\r\n" + ex.Message); });
            }
        }

        #region Properties
        public string Content
        {
            get
            {
                var content = string.Empty;
                Execute.OnUIThread(() =>
                {
                    lock (_lock)
                    {
                        if (_view == null)
                            content = string.Empty;
                        else
                            content = _view.TextEditor.Text;
                    }
                });

                return content;
            }
        }

        public string PublishedContent
        {
            get
            {
                var content = string.Empty;
                Execute.OnUIThread(() =>
                {
                    lock (_lock)
                    {
                        content = _view.PublishedTextEditor.Text;
                    }
                });

                return content;
            }
        }

        public string Tags
        {
            get
            {
                if (_runbook == null)
                    return string.Empty;

                return _runbook.Tags;
            }
            set
            {
                if (_runbook == null)
                    return;

                Execute.OnUIThread(() =>
                {
                    lock (_lock)
                    {
                        _runbook.Tags = value;
                    }
                });

                UnsavedChanges = true;
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

        public RunbookModelProxy Runbook
        {
            get { return _runbook; }
        }

        public IBackendContext Context
        {
            get { return _backendContext; }
        }

        public bool UnsavedChanges
        {
            get; set;
        }

        public override string DisplayName
        {
            get
            {
                string displayName = _runbook.RunbookName;

                if (String.IsNullOrEmpty(displayName))
                    displayName = "(untitled)";

                if (UnsavedChanges)
                    displayName += "*";

                return displayName;
            }
            set { }
        }

        public ICommand GoToDefinitionCommand
        {
            get { return AppContext.Resolve<ICommand>("GoToDefinitionCommand"); }
        }

        public IEnumerable<GitSharp.Diff.Section> DiffSectionA { get; set; }

        public IEnumerable<GitSharp.Diff.Section> DiffSectionB { get; set; }
        #endregion
    }
}
