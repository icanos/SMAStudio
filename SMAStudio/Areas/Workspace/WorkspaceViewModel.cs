using SMAStudio.Analysis;
using SMAStudio.Editor.Parsing;
using SMAStudio.Util;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace SMAStudio.ViewModels
{
    public class WorkspaceViewModel : ObservableObject, IWorkspaceViewModel, IDisposable
    {
        private IActiveRunbookParserService _parserService;
        private IParameterParserService _parameterParserService;
        private IErrorListViewModel _errorListViewModel;

        private string _title = "SMA Studio 2015";
        private string _customTitle = string.Empty;
        private string _statusBarText = string.Empty;

        public WorkspaceViewModel(IErrorListViewModel errorListViewModel, IActiveRunbookParserService parserService)
        {
            _errorListViewModel = errorListViewModel;
            _parserService = parserService;
            _parameterParserService = Core.Resolve<IParameterParserService>();

            StatusBarText = "SMA Studio 2014";
        }

        public void Initialize()
        {
            _parserService.Start();
            _parameterParserService.Start();
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
                    // If this is a newly created document and we click on it in the list
                    // of documents, we will end up with two of the same. To prevent that,
                    // we close the first one and use the last
                    var foundDocument = Documents.Where(d => d.ID.Equals(document.ID)).FirstOrDefault();
                    if (foundDocument != null)
                        Documents.Remove(foundDocument);

                    Documents.Add(document);
                    base.RaisePropertyChanged("Documents");

                    SelectedIndex = Documents.Count - 1;
                }
                else
                {
                    // Bring the selected document to the front
                    SelectedIndex = Documents.IndexOf(document);
                }
            });

            if (Documents[SelectedIndex].Content != null)
                _parserService.ParseCommandTokens(Documents[SelectedIndex]);
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

        public ICommand NewScheduleCommand
        {
            get { return Core.Resolve<ICommand>("NewSchedule"); }
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

            if (_parameterParserService != null)
                ((IDisposable)_parameterParserService).Dispose();
        }
    }
}
