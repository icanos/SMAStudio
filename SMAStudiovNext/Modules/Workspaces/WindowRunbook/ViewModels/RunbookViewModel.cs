using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Language;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;
using Caliburn.Micro;
using Gemini.Framework;
using Gemini.Framework.Commands;
using Gemini.Framework.Services;
using Gemini.Framework.Threading;
using Gemini.Modules.ErrorList;
using Gemini.Modules.Output;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using Newtonsoft.Json.Linq;
using SMAStudiovNext.Commands;
using SMAStudiovNext.Core;
using SMAStudiovNext.Exceptions;
using SMAStudiovNext.Models;
using SMAStudiovNext.Modules.DialogStartRun.Windows;
using SMAStudiovNext.Modules.WindowExecutionResult.ViewModels;
using SMAStudiovNext.Modules.WindowJobHistory.ViewModels;
using SMAStudiovNext.Modules.WindowRunbook.Editor;
using SMAStudiovNext.Modules.WindowRunbook.Editor.Completion;
using SMAStudiovNext.Modules.WindowRunbook.Editor.Debugging;
using SMAStudiovNext.Modules.WindowRunbook.Editor.Parser;
using SMAStudiovNext.Modules.WindowRunbook.Editor.Snippets;
using SMAStudiovNext.Modules.WindowRunbook.Views;
using SMAStudiovNext.SMA;
using SMAStudiovNext.Utils;
using SMAStudiovNext.Vendor.GitSharp;

namespace SMAStudiovNext.Modules.WindowRunbook.ViewModels
{
    public class RunbookViewModel : Document, IViewModel,
        ICommandHandler<SaveCommandDefinition>,
        ICommandHandler<PublishCommandDefinition>,
        ICommandHandler<EditPublishedCommandDefinition>,
        ICommandHandler<TestCommandDefinition>,
        ICommandHandler<RunCommandDefinition>,
        ICommandHandler<DebugCommandDefinition>,
        ICommandHandler<StopCommandDefinition>,
        IDisposable
    {
        private readonly IBackendContext _backendContext;
        private readonly IStatusManager _statusManager;
        private readonly DebuggerService _debuggerService;
        private readonly object _lock = new object();

        private BookmarkManager _bookmarkManager;
        private IList<ICompletionData> _parameters;
        private ICompletionProvider _completionProvider;
        private KeystrokeService _keystrokeService;
        private InsightService _insightService;
        private RunbookModelProxy _runbook;
        private IRunbookView _view;
        private bool _inTestRun = false;
        private bool _initialContentLoading = false;
        private DateTime _lastErrorUpdate = DateTime.MinValue;

        /// <summary>
        /// This variable is used mainly when creating a new runbook and
        /// a snippet is added to the runbook (default content). This may be
        /// done before the view has loaded, and therefore we can't apply it to the property
        /// Content since that would be null.
        /// </summary>
        private string _cachedDraftContent = string.Empty;

        private int _previousDebugLine = -1;
        private bool _isWaitingForDebugInput = false;

        public RunbookViewModel(RunbookModelProxy runbook)
        {
            _runbook = runbook;
            _backendContext = runbook.Context;
            _statusManager = AppContext.Resolve<IStatusManager>();
            _debuggerService = new DebuggerService(this);
            _debuggerService.DebuggerStopped += DebuggerStopped;
            _debuggerService.DebuggerFinished += DebuggerFinished;
        }

        private void DebuggerFinished(object sender, EventArgs e)
        {
            if (_view != null)
                _view.TextMarkerService.IsActiveDebugging = false;

            RemovePreviousDebugLine();
            NotifyOfPropertyChange("DisplayName");

            var output = IoC.Get<IOutput>();
            output.AppendLine("Debugging session completed.");

            Refresh();
        }

        private void DebuggerStopped(object sender, DebugEventArgs e)
        {
            var line = default(DocumentLine);
            _isWaitingForDebugInput = true;
            Refresh();
            
            Execute.OnUIThread(() =>
            {
                _view.TextMarkerService.IsActiveDebugging = true;
                NotifyOfPropertyChange("DisplayName");
                line = _view.TextEditor.Document.GetLineByNumber(e.LineNumber);

                // Make sure that the line is visible
                _view.TextEditor.TextArea.Caret.Line = line.LineNumber;
                _view.TextEditor.TextArea.Caret.BringCaretToView();

                // Check if a bookmark is found on the line
                var marker =
                    _view.TextMarkerService.TextMarkers.FirstOrDefault(x => x.Bookmark.LineNumber == e.LineNumber);
                if (marker != null)
                {
                    var output = IoC.Get<IOutput>();
                    output.AppendLine("Breakpoint at line " + e.LineNumber + " hit.");
                }
            });

            // Remove previous text marker
            RemovePreviousDebugLine();

            // Add a marker to the current line
            Execute.OnUIThread(() =>
            {
                var newMarker = new TextMarker(_view.TextMarkerService, line.Offset, (line.EndOffset - line.Offset))
                {
                    BackgroundColor = Colors.Gold,
                    ForegroundColor = Colors.Black,
                    Bookmark = new Bookmark(BookmarkType.CurrentDebugPoint, e.LineNumber)
                };
            
                _view.TextMarkerService.AddMarker(newMarker);
            });

            // Last
            _previousDebugLine = e.LineNumber;
            _isWaitingForDebugInput = false;
        }

        private void RemovePreviousDebugLine()
        {
            var previousLine = default(DocumentLine);

            Execute.OnUIThread(() =>
            {
                if (_previousDebugLine > -1)
                    previousLine = _view.TextEditor.Document.GetLineByNumber(_previousDebugLine);
            });

            // Remove previous text marker
            if (_previousDebugLine > -1)
            {
                var markers =
                    _view.TextMarkerService.GetMarkersAtOffset(previousLine.Offset)
                        .Where(item => item.Bookmark != null && (/*item.Bookmark.BookmarkType == BookmarkType.Breakpoint ||*/ item.Bookmark.BookmarkType == BookmarkType.CurrentDebugPoint))
                        .ToList();

                Execute.OnUIThread(() =>
                {
                    foreach (var marker in markers)
                        _view.TextMarkerService.Remove(marker);
                });
            }
        }

        private void BookmarkManagerOnBookmarkUpdated(object sender, BookmarkEventArgs bookmarkEventArgs)
        {
            if (bookmarkEventArgs.Bookmark.BookmarkType != BookmarkType.Breakpoint)
                return;

            if (bookmarkEventArgs.IsDeleted)
            {
                _debuggerService.RemoveBreakpoint(bookmarkEventArgs.Bookmark.LineNumber);

                var line = _view.TextEditor.Document.GetLineByNumber(bookmarkEventArgs.Bookmark.LineNumber);
                var markers = _view.TextMarkerService.GetMarkersAtOffset(line.Offset).Where(item => item.Bookmark != null && item.Bookmark.BookmarkType == BookmarkType.Breakpoint).ToList();

                Execute.OnUIThread(() =>
                {
                    foreach (var marker in markers)
                        _view.TextMarkerService.Remove(marker);
                });
            }
            else
            {
                _debuggerService.AddBreakpoint(bookmarkEventArgs.Bookmark.LineNumber);

                // Add a text marker as well
                var line = _view.TextEditor.Document.GetLineByNumber(bookmarkEventArgs.Bookmark.LineNumber);
                var marker = default(TextMarker);

                if (bookmarkEventArgs.Bookmark.TextMarker == null)
                {
                    marker = new TextMarker(_view.TextMarkerService, line.Offset, line.EndOffset - line.Offset);

                    marker.Bookmark = bookmarkEventArgs.Bookmark;
                    marker.BackgroundColor = Colors.DarkRed;
                    marker.ForegroundColor = Colors.White;

                    // Store the marker
                    bookmarkEventArgs.Bookmark.TextMarker = marker;
                    _view.TextMarkerService.AddMarker(marker);
                }
                else
                {
                    marker = bookmarkEventArgs.Bookmark.TextMarker;
                    marker.StartOffset = line.Offset;
                    marker.EndOffset = line.EndOffset;
                    marker.Length = line.Length;
                }
            }
        }

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

            // Clean up closing
            _initialContentLoading = false;

            _debuggerService.DebuggerStopped -= DebuggerStopped;
            _debuggerService.DebuggerFinished -= DebuggerFinished;
            _debuggerService.Dispose();

            ClearWarningsAndErrors();

            callback(true);
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

            AddSnippetInternal(content);
        }

        /// <summary>
        /// Internal snippet adding method, this is because when adding a snippet is called, the view
        /// may not be completly initialized and therefore unable to add the snippet at that time.
        /// </summary>
        /// <param name="content"></param>
        private void AddSnippetInternal(string content)
        {
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
            var editor = default(RunbookEditor);

            if (_view != null)
            {
                editor = (runbookType == RunbookType.Draft) ? _view.TextEditor : _view.PublishedTextEditor;
            }

            return AsyncHelper.RunSync<string>(() => GetContentInternalAsync(editor, runbookType, forceDownload));
        }

        private string GetContentInternal(RunbookEditor editor, RunbookType runbookType, bool forceDownload)
        {
            return AsyncHelper.RunSync<string>(() => GetContentInternalAsync(editor, runbookType, forceDownload));
        }

        /// <summary>
        /// Get content from our backend service or cache
        /// </summary>
        /// <param name="forceDownload">Set to true to force download of content from backend</param>
        /// <returns>Draft content</returns>
        private async Task<string> GetContentInternalAsync(RunbookEditor editor, RunbookType runbookType, bool forceDownload)
        {
            var content = string.Empty;
            var currentContent = string.Empty;
            var output = IoC.Get<IOutput>();

            // Get current content
            Execute.OnUIThread(() => { if (editor != null) { currentContent = editor.Text; } output.AppendLine("Loading " + runbookType + " content for " + _runbook.RunbookName); });

            if (forceDownload || String.IsNullOrEmpty(currentContent.Trim()))
            {
                try
                {
                    content = await _backendContext.GetContentAsync(_backendContext.Service.GetBackendUrl(runbookType, _runbook)).ConfigureAwait(true);

                    // Only parse draft, we don't handle published since these are read only
                    if (runbookType == RunbookType.Draft && _completionProvider != null)
                        _completionProvider.Context.Parse(content);

                    // Make sure that it's not XML that we receive. If that is the case, display a notification in the output window
                    if (content.StartsWith("<string xmlns=\""))
                    {
                        // It's XML
                        var xml = new XmlDocument();
                        xml.LoadXml(content);

                        var json = xml.InnerText;
                        dynamic jsonData = JObject.Parse(json);

                        //MessageBox.Show(jsonData.message.ToString(), "Information");
                        output.AppendLine(jsonData.message.ToString());
                    }
                    else
                    {
                        if (editor != null)
                        {
                            Execute.OnUIThread(() =>
                            {
                                //lock (_lock)
                                //{
                                editor.Text = content;
                                //}
                            });
                        }
                    }

                    Execute.OnUIThread(() => { output.AppendLine("Content loaded."); });
                }
                catch (ApplicationException ex)
                {
                    Logger.Error("Exception when loading data!", ex);
                    GlobalExceptionHandler.Show(ex);
                }
            }
            else
            {
                if (editor != null)
                {
                    Execute.OnUIThread(() =>
                    {
                        //lock (_lock)
                       // {
                        content = editor.Text;
                        //}
                    });
                }
            }

            _initialContentLoading = true;

            return content;
        }
        
        /// <summary>
        /// Gets called when the view is loaded
        /// </summary>
        /// <param name="view"></param>
        protected override void OnViewLoaded(object view)
        {
            Logger.DebugFormat("OnViewLoaded(...)");

            _view = (IRunbookView)view;
            _completionProvider = new CompletionProvider(_backendContext, _view.TextEditor.LanguageContext);
            Task.Run(() => { _completionProvider.Initialize(); });

            _bookmarkManager = new BookmarkManager(_view.TextMarkerService);
            _bookmarkManager.OnBookmarkUpdated += BookmarkManagerOnBookmarkUpdated;

            _keystrokeService = new KeystrokeService(this, _view.TextEditor.TextArea, _completionProvider,
                _view.TextEditor.LanguageContext, _debuggerService, _bookmarkManager);
            _view.TextEditor.TextArea.LeftMargins.Insert(0, new IconBarMargin(_bookmarkManager));

            // Add a bookmarks line tracker
            _view.TextEditor.TextArea.Document.LineTrackers.Add(new BookmarkLineTracker(_view.TextEditor.TextArea, _bookmarkManager));

            _insightService = new InsightService(_view.TextEditor, _view.TextEditor.LanguageContext, _debuggerService);

            // Attach the parse error event handler
            _view.TextEditor.OnTextInputCompleted += TextEditor_OnTextInputCompleted;
            _view.TextEditor.LanguageContext.OnParseError += OnDraftParseError;
            _view.TextEditor.LanguageContext.OnClearParseErrors += OnClearDraftParseErrors;
            _view.TextEditor.LanguageContext.OnAnalysisCompleted += OnDraftAnalysisCompleted;
            //_view.TextEditor.ToolTipRequest += OnToolTipRequest;

            if (_runbook.RunbookID != Guid.Empty)
            {
                Task.Run(async () =>
                {
                    if (_runbook.DraftRunbookVersionID.HasValue)
                        await GetContentInternalAsync(_view.TextEditor, RunbookType.Draft, true).ConfigureAwait(false);

                    if (_runbook.PublishedRunbookVersionID.HasValue)
                        await GetContentInternalAsync(_view.PublishedTextEditor, RunbookType.Published, true).ConfigureAwait(false);

                    var draftContent = string.Empty;// _view.TextEditor.Text;
                    var publishedContent = string.Empty;// _view.PublishedTextEditor.Text;

                    Execute.OnUIThread(() =>
                    {
                        draftContent = _view.TextEditor.Text;
                        publishedContent = _view.PublishedTextEditor.Text;
                    });

                    ParseContent();

                    var diff = new Diff(draftContent, publishedContent);
                    DiffSectionA = diff.Sections;
                    DiffSectionB = diff.Sections;

                    NotifyOfPropertyChange(() => DiffSectionA);
                    NotifyOfPropertyChange(() => DiffSectionB);
                });
            }
            else
            {
                // We are now ready to insert any snippets
                if (!String.IsNullOrEmpty(_cachedDraftContent))
                {
                    AddSnippetInternal(_cachedDraftContent);
                    _cachedDraftContent = string.Empty;
                }
            }

            _view.TextEditor.TextChanged += delegate (object sender, EventArgs e)
            {
                if (_initialContentLoading)
                {
                    UnsavedChanges = true;
                }
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

        private void TextEditor_OnTextInputCompleted(object sender, EventArgs e)
        {
            ParseContent();
        }

        private void OnDraftAnalysisCompleted(object sender, AnalysisEventArgs e)
        {
            //if ((DateTime.Now - LastKeyStroke).Seconds < 2)
            //    return;
            var addedAndFoundMarkers = new List<Bookmark>();

            foreach (var record in e.Records)
            {
                var bookmarkType = default(BookmarkType);

                switch (record.Severity)
                {
                    case Microsoft.Windows.PowerShell.ScriptAnalyzer.Generic.DiagnosticSeverity.Information:
                        bookmarkType = BookmarkType.AnalyzerInfo;
                        break;
                    case Microsoft.Windows.PowerShell.ScriptAnalyzer.Generic.DiagnosticSeverity.Warning:
                        bookmarkType = BookmarkType.AnalyzerWarning;
                        break;
                }

                // Try to find a bookmark for this record
                var foundMarker = _bookmarkManager.Bookmarks.FirstOrDefault(x =>
                    x.LineNumber == record.Extent.StartLineNumber &&
                    x.BookmarkType.Equals(bookmarkType));// &&
                    //x.TextMarker != null &&
                    //x.TextMarker.StartOffset == record.Extent.StartOffset);

                if (foundMarker != null)
                {
                    addedAndFoundMarkers.Add(foundMarker);
                    continue;
                }

                Execute.OnUIThread(() =>
                {
                    var bookmark = new Bookmark(
                        bookmarkType,
                        _view.TextMarkerService,
                        record.Extent.StartLineNumber,
                        record.Extent.StartColumnNumber,
                        record.Extent.StartOffset,
                        record.Extent.EndOffset - record.Extent.StartOffset,
                        record.Message,
                        _runbook.RunbookName);

                    if (_bookmarkManager.Add(bookmark))
                        addedAndFoundMarkers.Add(bookmark);
                });
            }

            // Remove any deprecated warnings/errors
            var bookmarksToRemove = _bookmarkManager.Bookmarks.Where(x => (x.BookmarkType == BookmarkType.AnalyzerInfo || x.BookmarkType == BookmarkType.AnalyzerWarning) && !addedAndFoundMarkers.Contains(x)).ToList();
            foreach (var bookmark in bookmarksToRemove)
            {
                Execute.OnUIThread(() => bookmark.CleanUp());
                _bookmarkManager.Bookmarks.Remove(bookmark);
            }

            addedAndFoundMarkers.Clear();
        }

        private void OnClearDraftParseErrors(object sender, EventArgs e)
        {
            // Remove all parse errors
            var bookmarksToRemove = _bookmarkManager.Bookmarks.Where(x => x.BookmarkType == BookmarkType.ParseError).ToList();
            foreach (var bookmark in bookmarksToRemove)
            {
                Execute.OnUIThread(() => bookmark.CleanUp());
                _bookmarkManager.Bookmarks.Remove(bookmark);
            }
        }
        
        private void OnDraftParseError(object sender, ParseErrorEventArgs e)
        {
            var addedAndFoundMarkers = new List<Bookmark>();

            foreach (var record in e.Errors)
            {
                // Try to find a bookmark for this record
                var foundMarker = _bookmarkManager.Bookmarks.FirstOrDefault(x =>
                    x.LineNumber == record.Extent.StartLineNumber &&
                    x.BookmarkType.Equals(BookmarkType.ParseError) &&
                    x.TextMarker != null &&
                    x.TextMarker.StartOffset == record.Extent.StartOffset);

                if (foundMarker != null)
                {
                    addedAndFoundMarkers.Add(foundMarker);
                    continue;
                }

                Execute.OnUIThread(() =>
                {
                    var bookmark = new Bookmark(
                        BookmarkType.ParseError,
                        _view.TextMarkerService,
                        record.Extent.StartLineNumber,
                        record.Extent.StartColumnNumber,
                        record.Extent.StartOffset,
                        record.Extent.EndOffset - record.Extent.StartOffset,
                        record.Message,
                        _runbook.RunbookName);

                    if (_bookmarkManager.Add(bookmark))
                        addedAndFoundMarkers.Add(bookmark);
                });
            }

            // Remove any deprecated warnings/errors
            var bookmarksToRemove = _bookmarkManager.Bookmarks.Where(x => (x.BookmarkType == BookmarkType.ParseError) && !addedAndFoundMarkers.Contains(x)).ToList();
            foreach (var bookmark in bookmarksToRemove)
            {
                Execute.OnUIThread(() => bookmark.CleanUp());
                _bookmarkManager.Bookmarks.Remove(bookmark);
            }

            addedAndFoundMarkers.Clear();
        }

        public void ClearWarningsAndErrors()
        {
            var items = _bookmarkManager.Bookmarks.Where(x => x.ErrorListItem != null && x.ErrorListItem.File.Equals(Runbook.RunbookName)).ToList();

            foreach (var item in items)
            {
                Execute.OnUIThread(() => item.CleanUp());
                _bookmarkManager.Bookmarks.Remove(item);
            }
        }

        /// <summary>
        /// Called when the user uses ctrl+space command in the textarea
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCtrlSpaceCommand(object sender, ExecutedRoutedEventArgs e)
        {
            //ShowCompletion(completionWord: "", controlSpace: true);
            _keystrokeService.TriggerCompletion();
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

        public Token GetCurrentContext()
        {
            var caretOffset = 0;
            var line = default(DocumentLine);

            if (_view == null)
                return null;

            Execute.OnUIThread(() => { caretOffset = _view.TextEditor.CaretOffset; line = _view.TextEditor.Document.GetLineByOffset(caretOffset); });

            return _completionProvider.Context.GetContext(line.LineNumber, caretOffset).LastOrDefault();
        }

        public void ParseContent()
        {
            if (_view == null)
                return;

            var contentToParse = string.Empty;
            var context = default(LanguageContext);

            try
            {
                Execute.OnUIThread(() =>
                {
                    contentToParse = _view.TextEditor.Text;
                    context = _view.TextEditor.LanguageContext;
                });
            }
            catch (TaskCanceledException) { }

            context?.Parse(contentToParse);
        }
        
        /// <summary>
        /// TODO: Async this!
        /// </summary>
        /// <param name="completionWord"></param>
        /// <returns></returns>
        public IList<ICompletionData> GetParameters(string completionWord)
        {
            if (_parameters != null) // check if parameters is cached
                return _parameters;

            var completionEntries = new List<ICompletionData>();
            var fixedCompletionWord = completionWord?.Replace("-", "");

            // Check if the content's already been loaded
            ScriptBlockAst scriptBlock = null;
            Execute.OnUIThread(() =>
            {
                if (!string.IsNullOrEmpty(_view?.TextEditor.Text))
                {
                    scriptBlock = _view.TextEditor.LanguageContext.ScriptBlock;
                }
            });
            
            if (scriptBlock == null)
            {
                var contentToParse = GetContentInternal(null,
                    _runbook.DraftRunbookVersionID.HasValue ? RunbookType.Draft : RunbookType.Published, false);

                Token[] tokens;
                ParseError[] parseErrors;

                scriptBlock = System.Management.Automation.Language.Parser.ParseInput(contentToParse, out tokens, out parseErrors);
            }
            
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
                        var isMandatory = false;
                        AttributeBaseAst attrib = null;

                        if (param.Attributes.Count > 0)
                            attrib = param.Attributes[param.Attributes.Count - 1]; // always the last one

                        if (fixedCompletionWord != null && !param.Name.Extent.Text.Substring(1).StartsWith(fixedCompletionWord, StringComparison.InvariantCultureIgnoreCase))
                            continue;

                        if (param.Attributes.Count > 1)
                        {
                            // Probably contains a Parameter(Mandatory = ...) or something, check it out
                            var ast = param.Attributes[0] as AttributeAst;
                            if (ast != null)
                            {
                                if (ast.Extent.Text.Contains("[Parameter") ||
                                    ast.Extent.Text.Contains("[parameter"))
                                {
                                    foreach (var namedParameter in ast.NamedArguments.Where(namedParameter => namedParameter.ArgumentName.Equals("Mandatory", StringComparison.InvariantCultureIgnoreCase)))
                                    {
                                        isMandatory = namedParameter.Argument.Extent.Text.Equals("$true") ? true : false;
                                        break;
                                    }
                                }
                            }
                        }

                        var input = new ParameterCompletionData
                        {
                            RawName = param.Name.Extent.Text.Substring(1),
                            DisplayText = "-" + ConvertToNiceName(param.Name.Extent.Text) + (isMandatory ? " (required)" : ""),
                            Name = "-" + param.Name.Extent.Text.Substring(1),                  // Remove the $
                            IsArray = (attrib != null && attrib.TypeName.IsArray ? true : false),
                            Type = attrib != null ? attrib.TypeName.Name : "",
                            IsRequired = isMandatory
                        };

                        if (attrib != null &&
                            attrib.TypeName.Name.Equals("bool", StringComparison.InvariantCultureIgnoreCase))
                            input.Value = false;
                        
                        completionEntries.Add(input);
                    }
                    catch (Exception ex)
                    {
                        var output = IoC.Get<IOutput>();
                        output.AppendLine("Error parsing parameters: " + ex);
                    }
                }
            }

            if (_parameters == null)
                _parameters = completionEntries;

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

            try
            {
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
            catch (ApplicationException ex)
            {
                GlobalExceptionHandler.Show(ex);
            }
        }

        public async Task CheckOut()
        {
            try
            {
                var result = await _backendContext.Service.CheckOut(this).ConfigureAwait(false);

                if (result)
                {
                    Execute.OnUIThread(() =>
                    {
                        _view.TextEditor.Text = _view.PublishedTextEditor.Text;
                    });
                }
            }
            catch (ApplicationException ex)
            {
                GlobalExceptionHandler.Show(ex);
            }
        }

        private async Task Save(Command command)
        {
            // Update the UI to notify that the changes has been saved
            Execute.OnUIThread(() => { UnsavedChanges = false; });

            try
            {
                await _backendContext.Service.Save(this, command).ConfigureAwait(false);

                _runbook.ViewModel = this;
                _backendContext.AddToRunbooks(_runbook);
            }
            catch (ApplicationException ex)
            {
                GlobalExceptionHandler.Show(ex);
            }

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

        Task ICommandHandler<StopCommandDefinition>.Run(Command command)
        {
            _debuggerService.Stop();
            NotifyOfPropertyChange("DisplayName");

            return TaskUtility.Completed;
        }

        void ICommandHandler<StopCommandDefinition>.Update(Command command)
        {
            if (_debuggerService != null && _debuggerService.IsActiveDebugging)
                command.Enabled = true;
            else
                command.Enabled = false;
        }

        async Task ICommandHandler<DebugCommandDefinition>.Run(Command command)
        {
            var dialog = new PrepareRunWindow(this);
            var result = (bool)dialog.ShowDialog();

            if (!result)
                return;

            var output = IoC.Get<IOutput>();
            output.AppendLine("    ");
            output.AppendLine("Starting a debug session...");

            var inputs =
                // ReSharper disable once PossibleNullReferenceException
                dialog.Inputs.Select(input => new KeyValuePair<string, object>(input.Text, (input as ParameterCompletionData).Value)).ToList();

            await _debuggerService.Start(inputs);
            NotifyOfPropertyChange("DisplayName");
            
            //return TaskUtility.Completed;
        }

        void ICommandHandler<DebugCommandDefinition>.Update(Command command)
        {
            if (_runbook.DraftRunbookVersionID.HasValue)
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

            // Make sure that draft runbook version ID is set to null when published
            _runbook.DraftRunbookVersionID = null;
            UnsavedChanges = false;
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

            UnsavedChanges = true;
        }

        void ICommandHandler<TestCommandDefinition>.Update(Command command)
        {
            if (_runbook.DraftRunbookVersionID.HasValue && !_inTestRun && (_debuggerService == null || !_debuggerService.IsActiveDebugging))
                command.Enabled = true;
            else
                command.Enabled = false;
        }

        async Task ICommandHandler<TestCommandDefinition>.Run(Command command)
        {
            if (!_runbook.DraftRunbookVersionID.HasValue || String.IsNullOrEmpty(Content))
            {
                if (String.IsNullOrEmpty(Content))
                    MessageBox.Show("The runbook is empty, please create a workflow before trying to run.", "Empty runbook", MessageBoxButton.OK, MessageBoxImage.Information);

                return;
            }

            _inTestRun = true;
            command.Enabled = false;

            await StartRunAsync(command, true);

            command.Enabled = true;
            _inTestRun = false;
        }

        void ICommandHandler<RunCommandDefinition>.Update(Command command)
        {
            // If we're in a debugging session, the button is only active if we're "waiting" for user input, eg step in/over
            if (_debuggerService != null && _debuggerService.IsActiveDebugging)
            {
                if (_isWaitingForDebugInput)
                    command.Enabled = true;

                return;
            }

            // If we're not debugging, only enable Run when a published runbook exists AND the content is not empty
            if (!_runbook.PublishedRunbookVersionID.HasValue || string.IsNullOrEmpty(PublishedContent))
                command.Enabled = false;
            else
            {
                command.Enabled = true;
            }
        }

        async Task ICommandHandler<RunCommandDefinition>.Run(Command command)
        {
            if (_debuggerService != null && _debuggerService.IsActiveDebugging && _isWaitingForDebugInput)
            {
                _debuggerService.Continue();
                return;
            }

            if (!_runbook.PublishedRunbookVersionID.HasValue || String.IsNullOrEmpty(_view.PublishedTextEditor.Text))
            {
                if (String.IsNullOrEmpty(Content))
                    MessageBox.Show("The published runbook is empty, please create a workflow before trying to run.", "Empty runbook", MessageBoxButton.OK, MessageBoxImage.Information);

                return;
            }

            command.Enabled = false;
            Refresh();

            await StartRunAsync(command, false).ConfigureAwait(true);

            command.Enabled = true;
            Refresh();
        }

        private async Task StartRunAsync(Command command, bool isDraft)
        {
            var dialog = new PrepareRunWindow(this);
            var result = (bool)dialog.ShowDialog();

            if (!result)
                return;

            if (UnsavedChanges)
                await Save(command).ConfigureAwait(true);

            try
            {
                var runningJob = await _backendContext.Service.CheckRunningJobs(_runbook, isDraft).ConfigureAwait(true);
                if (runningJob)
                {
                    MessageBox.Show("There is currently a running job, please wait for it to finish.", "Running Jobs", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
            }
            catch (ApplicationException ex)
            {
                GlobalExceptionHandler.Show(ex);
            }

            // Convert to NameValuePair
            var parameters = new List<NameValuePair>();

            foreach (var param in dialog.Inputs)
            {
                var pair = new NameValuePair();
                pair.Name = (param as ParameterCompletionData).RawName;
                pair.Value = ((ParameterCompletionData)param).Value.ToString();

                parameters.Add(pair);
            }

            // Open the execution result window
            var executionViewModel = new ExecutionResultViewModel(this, isDraft);
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

                    try
                    {
                        if (isDraft)
                            guid = _backendContext.Service.TestRunbook(_runbook, parameters);
                        else
                            guid = _backendContext.Service.StartRunbook(_runbook, parameters);
                    }
                    catch (ApplicationException ex)
                    {
                        GlobalExceptionHandler.Show(ex);
                    }

                    if (guid.HasValue)
                        _runbook.JobID = guid.Value;
                }).ConfigureAwait(true);

                if (_runbook.JobID == Guid.Empty)
                    Execute.OnUIThread(() => { shell.CloseDocument(executionViewModel); });
            }
            catch (Exception ex)
            {
                Execute.OnUIThread(() => { output.AppendLine("Error when trying to " + (isDraft ? "test" : "run") + " the runbook:\r\n" + ex.Message); });
            }
        }

        #region Properties
        public DateTime LastKeyStroke { get; set; }

        public string Content
        {
            get
            {
                var content = string.Empty;
                Execute.OnUIThread(() =>
                {
                    //lock (_lock)
                    //{
                        if (_view == null)
                            content = string.Empty;
                        else
                            content = _view.TextEditor.Text;
                    //}
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

        private bool _unsavedChanges = false;
        public bool UnsavedChanges
        {
            get { return _unsavedChanges; }
            set
            {
                _unsavedChanges = value;
                NotifyOfPropertyChange(() => DisplayName);
            }
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

                if (_debuggerService != null && _debuggerService.IsActiveDebugging)
                    displayName += " (debugging)";

                return displayName;
            }
            set { }
        }

        public ICommand GoToDefinitionCommand
        {
            get { return AppContext.Resolve<ICommand>("GoToDefinitionCommand"); }
        }

        public IEnumerable<Diff.Section> DiffSectionA { get; set; }

        public IEnumerable<Diff.Section> DiffSectionB { get; set; }
        #endregion

        protected override void OnDeactivate(bool close)
        {
            if (close)
                Dispose();

            base.OnDeactivate(close);
        }

        public void Dispose()
        {
            // This is needed in case we're running a debugging session and closing the runbook/application.
            // If this was not called, the application would hang in the background since we'd be waiting for a
            // task to complete which we don't have access to anymore.
            _debuggerService.DebuggerStopped -= DebuggerStopped;
            _debuggerService.DebuggerFinished -= DebuggerFinished;
            _debuggerService.Dispose();
        }
    }
}
