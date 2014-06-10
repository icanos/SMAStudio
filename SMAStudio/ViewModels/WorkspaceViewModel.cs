using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Editing;
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
    public class WorkspaceViewModel : ObservableObject, IDisposable
    {
        private CompletionEngine _codeCompletionEngine;
        private CompletionWindow _completionWindow;

        private ParserService _parserService;
        private ErrorListViewModel _errorListViewModel;
        private ComponentsViewModel _componentsViewModel;

        private ICommand _saveCommand;
        private ICommand _findCommand;
        private ICommand _closeCommand;
        private ICommand _closeAllCommand;
        private ICommand _newCredentialCommand;
        private ICommand _newRunbookCommand;
        private ICommand _newVariableCommand;
        private ICommand _exitCommand;

        private string _title = "SMA Studio 2014";
        private string _customTitle = string.Empty;
        private string _statusBarText = string.Empty;

        public WorkspaceViewModel(ErrorListViewModel errorListViewModel)
        {
            _errorListViewModel = errorListViewModel;
            StatusBarText = "SMA Studio 2014";

            /*Documents.Add(new RunbookViewModel()
            {
                CheckedOut = false,
                Content = "",
                Runbook = new SMAWebService.Runbook()
            });

            SelectedIndex = 0;*/

            _saveCommand = new SaveCommand(null);
            _findCommand = new FindCommand();
            _closeCommand = new CloseCommand(this);
            _closeAllCommand = new CloseAllCommand(this);
            _newCredentialCommand = new NewCredentialCommand(this);
            _newRunbookCommand = new NewRunbookCommand(this);
            _newVariableCommand = new NewVariableCommand(this);
            _exitCommand = new ExitCommand();

            _codeCompletionEngine = new CompletionEngine();
            //_codeCompletionEngine.Start();

            _parserService = new ParserService(this, _errorListViewModel);
            _parserService.Start();
        }

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
        }

        private string _cachedText = string.Empty;
        public void EditorTextEntered(object sender, TextCompositionEventArgs e)
        {
            if (e.Text == "-" && _codeCompletionEngine.ApprovedVerbs.Contains(_cachedText))
            {
                if (_cachedText.StartsWith("$"))
                {
                    _cachedText = string.Empty;
                    return;
                }

                var textArea = ((TextArea)sender);

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
        }

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

        private ObservableCollection<IDocumentViewModel> _documents = new ObservableCollection<IDocumentViewModel>();
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
        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set { _selectedIndex = value; base.RaisePropertyChanged("SelectedIndex"); }
        }

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

        public IDocumentViewModel CurrentDocument
        {
            get { return SelectedIndex < 0 || Documents.Count == 0 ? null : Documents[SelectedIndex]; }
        }

        public ICommand SaveCommand
        {
            get { return _saveCommand; }
        }

        public ICommand FindCommand
        {
            get { return _findCommand; }
        }

        public ICommand CloseCommand
        {
            get { return _closeCommand; }
        }

        public ICommand CloseAllCommand
        {
            get { return _closeAllCommand; }
        }

        public ICommand NewCredentialCommand
        {
            get { return _newCredentialCommand; }
        }

        public ICommand NewRunbookCommand
        {
            get { return _newRunbookCommand; }
        }

        public ICommand NewVariableCommand
        {
            get { return _newVariableCommand; }
        }

        public ICommand ExitCommand
        {
            get { return _exitCommand; }
        }

        public ComponentsViewModel Components
        {
            get { return _componentsViewModel; }
            set
            {
                _componentsViewModel = value;
                _saveCommand = new SaveCommand(_componentsViewModel);
            }
        }

        public void Dispose()
        {
            if (_parserService != null)
                _parserService.Dispose();
        }
    }
}
