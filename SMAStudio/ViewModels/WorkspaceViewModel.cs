using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Snippets;
using Microsoft.Practices.Unity;
using SMAStudio.Commands;
using SMAStudio.Editor.CodeCompletion;
using SMAStudio.Editor.Parsing;
using SMAStudio.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace SMAStudio.ViewModels
{
    /// <summary>
    /// WorkspaceViewModel is current dependent of MvvmTextEditor since there are bindings between these
    /// in order for Code Completion to work. This is not compliant with MVVM.
    /// </summary>
    public class WorkspaceViewModel : ObservableObject, IWorkspaceViewModel, IDisposable
    {
        private CompletionEngine _codeCompletionEngine;
        private CompletionWindow _completionWindow;

        private IParserService _parserService;
        private IErrorListViewModel _errorListViewModel;

        private string _title = "SMA Studio 2014";
        private string _customTitle = string.Empty;
        private string _statusBarText = string.Empty;

        public WorkspaceViewModel(IErrorListViewModel errorListViewModel, IParserService parserService)
        {
            _errorListViewModel = errorListViewModel;
            _parserService = parserService;

            StatusBarText = "SMA Studio 2014";

            _codeCompletionEngine = new CompletionEngine();
            //_codeCompletionEngine.Start();
        }

        public void Initialize()
        {
            _parserService.Start();
        }

        /// <summary>
        /// Opens a new document and changes focus to that document
        /// </summary>
        /// <param name="document"></param>
        public void OpenDocument(IDocumentViewModel document)
        {
            if (App.Current == null)
                return;

            App.Current.Dispatcher.Invoke((Action)delegate()
            {
                if (!Documents.Contains(document))
                {
                    Documents.Add(document);
                    base.RaisePropertyChanged("Documents");

                    SelectedIndex = Documents.Count - 1;
                }
            });

            _parserService.ParseCommandTokens(Documents[SelectedIndex]);
        }

        private string _cachedText = string.Empty;
        /// <summary>
        /// Event called after text has been added to the editor text box
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void EditorTextEntered(object sender, TextCompositionEventArgs e)
        {
            var textArea = ((TextArea)sender);

            if (e.Text == "-" && _codeCompletionEngine.ApprovedVerbs.Contains(_cachedText))
            {
                if (_cachedText.StartsWith("$"))
                {
                    _cachedText = string.Empty;
                    return;
                }

                //var textArea = ((TextArea)sender);

                _completionWindow = new CompletionWindow(textArea);
                _completionWindow.Width = 300;

                IList<ICompletionData> data = _completionWindow.CompletionList.CompletionData;
                string wordBefore = StringHelper.FindWordBeforeDash(_cachedText) + "-";

                if (wordBefore.Equals("-"))
                {
                    // This means that we are adding a parameter
                    _cachedText = string.Empty;
                    return;
                }

                foreach (var cmd in _codeCompletionEngine.Commands)
                {
                    ((CompletionSnippet)cmd).ReplaceText = wordBefore;

                    if (cmd.Text.StartsWith(wordBefore, StringComparison.InvariantCultureIgnoreCase))
                        data.Add(cmd);
                }
                
                //_completionWindow.Show();
                _completionWindow.Closed += delegate
                {
                    _completionWindow = null;
                    _cachedText = string.Empty;
                };
            }
            else
            {
                if (e.Text == " ")
                    _cachedText = string.Empty;
                else if (e.Text == "\n")
                    _cachedText = string.Empty;
                else
                    _cachedText += e.Text;
            }

            _parserService.ParseCommandTokens(Documents[SelectedIndex]);
        }

        /// <summary>
        /// Event called when text is being entered into the editor text box
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void EditorTextEntering(object sender, TextCompositionEventArgs e)
        {
            if (e.Text.Length > 0 && _completionWindow != null)
            {
                if (e.Text == " " || e.Text == "\t")
                {
                    _completionWindow.CompletionList.RequestInsertion(e);

                    if (e.Text == "\t")
                        e.Handled = true;
                }
            }
        }

        #region Properties
        private ObservableCollection<IDocumentViewModel> _documents = new ObservableCollection<IDocumentViewModel>();
        /// <summary>
        /// Gets or sets documents that are open
        /// </summary>
        public ObservableCollection<IDocumentViewModel> Documents
        {
            get { return _documents; }
            set
            {
                _documents = value;
                base.RaisePropertyChanged("Documents");
            }
        }

        private int _selectedIndex = 0;
        /// <summary>
        /// Gets or sets which document is active in the editor pane
        /// </summary>
        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set { _selectedIndex = value; base.RaisePropertyChanged("SelectedIndex"); }
        }

        /// <summary>
        /// Gets or sets the window title
        /// </summary>
        public string WindowTitle
        {
            get
            {
                if (!String.IsNullOrEmpty(_customTitle))
                    return _customTitle + " - " + _title;

                return _title;
            }
            set
            {
                if (App.Current == null)
                    return;

                App.Current.Dispatcher.Invoke((Action)delegate()
                {
                    _customTitle = value;
                    base.RaisePropertyChanged("WindowTitle");
                });
            }
        }

        /// <summary>
        /// Gets or sets the status bar text
        /// </summary>
        public string StatusBarText
        {
            get { return _statusBarText; }
            set
            {
                if (App.Current == null)
                    return;

                App.Current.Dispatcher.Invoke((Action)delegate()
                {
                    _statusBarText = value;
                    base.RaisePropertyChanged("StatusBarText");
                });
            }
        }

        /// <summary>
        /// Gets the current active document
        /// </summary>
        public IDocumentViewModel CurrentDocument
        {
            get { return SelectedIndex < 0 || Documents.Count == 0 ? null : Documents[SelectedIndex]; }
        }

        public ICommand SaveCommand
        {
            get { return Core.Resolve<ICommand>("Save"); }
        }

        public ICommand FindCommand
        {
            get { return Core.Resolve<ICommand>("Find"); }
        }

        public ICommand CloseCommand
        {
            get { return Core.Resolve<ICommand>("Close"); }
        }

        public ICommand CloseAllCommand
        {
            get { return Core.Resolve<ICommand>("CloseAll"); }
        }

        public ICommand NewCredentialCommand
        {
            get { return Core.Resolve<ICommand>("NewCredential"); }
        }

        public ICommand NewRunbookCommand
        {
            get { return Core.Resolve<ICommand>("NewRunbook"); }
        }

        public ICommand NewVariableCommand
        {
            get { return Core.Resolve<ICommand>("NewVariable"); }
        }

        public ICommand ExitCommand
        {
            get { return Core.Resolve<ICommand>("Exit"); }
        }

        public ICommand AboutCommand
        {
            get { return Core.Resolve<ICommand>("About"); }
        }
        #endregion

        public void Dispose()
        {
            if (_parserService != null)
                ((IDisposable)_parserService).Dispose();
        }
    }
}
