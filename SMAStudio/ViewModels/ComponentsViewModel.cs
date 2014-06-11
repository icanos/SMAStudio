using SMAStudio.Commands;
using SMAStudio.Util;
using SMAStudio.SMAWebService;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data.Services.Client;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Threading;
using System.Windows;
using SMAStudio.Services;

namespace SMAStudio.ViewModels
{
    public class ComponentsViewModel : ObservableObject
    {
        private ICommand _loadCommand;
        private ICommand _checkInCommand;
        private ICommand _checkOutCommand;
        private ICommand _newRunbookCommand;
        private ICommand _newVariableCommand;
        private ICommand _newCredentialCommand;
        private ICommand _deleteCommand;

        private IDocumentViewModel _selectedRunbook = null;
        private WorkspaceViewModel _dataContext;

        private RunbookService _runbookService;
        private VariableService _variableService;
        private CredentialService _credentialService;

        private object _sync = new object();
        
        public ComponentsViewModel(WorkspaceViewModel dataContext)
        {
            _dataContext = dataContext;

            Runbooks = new ObservableCollection<RunbookViewModel>();
            Variables = new ObservableCollection<VariableViewModel>();
            Credentials = new ObservableCollection<CredentialViewModel>();

            _loadCommand = new LoadCommand(_dataContext, this);
            _checkInCommand = new CheckInCommand();
            _checkOutCommand = new CheckOutCommand();
            _newRunbookCommand = new NewRunbookCommand(dataContext);
            _newVariableCommand = new NewVariableCommand(dataContext);
            _newCredentialCommand = new NewCredentialCommand(dataContext);
            _deleteCommand = new DeleteCommand(this, dataContext);

            _runbookService = new RunbookService();
            _variableService = new VariableService();
            _credentialService = new CredentialService();

            Load();
        }

        public void Load(bool forceDownload = false)
        {
            _dataContext.StatusBarText = "Retrieving data from SMA...";

            AsyncService.Execute(ThreadPriority.Normal, delegate()
            {
                Core.Log.DebugFormat("Loading runbooks...");

                // Load the runbooks
                Runbooks = _runbookService.GetRunbookViewModels(forceDownload);
                base.RaisePropertyChanged("Runbooks");

                // After the runbooks has been loaded, we can scan through each
                // runbook and load the different versions of it.
                lock (_sync)
                {
                    foreach (var runbook in Runbooks)
                    {
                        // Only retrieve the different versions for checked out runbooks
                        if (!runbook.CheckedOut)
                            continue;

                        Core.Log.DebugFormat("Versions being loaded for {0}", runbook.Runbook.RunbookID);

                        runbook.Versions = _runbookService.GetVersions(runbook);
                        runbook.LoadedVersions = true;
                    }
                }
                base.RaisePropertyChanged("Runbooks");

                //MainWindow.Instance.SetStatusBarInformation("Connected");
                _dataContext.StatusBarText = "Connected";
            });

            if (_runbookService.SuccessfulInitialization)
            {
                AsyncService.Execute(ThreadPriority.Normal, delegate()
                {
                    Core.Log.DebugFormat("Loading variables...");

                    Variables = _variableService.GetVariableViewModels(forceDownload);
                    base.RaisePropertyChanged("Variables");
                });
            }

            if (_runbookService.SuccessfulInitialization || _variableService.SuccessfulInitialization)
            {
                AsyncService.Execute(ThreadPriority.Normal, delegate()
                {
                    Core.Log.DebugFormat("Loading credentials...");

                    Credentials = _credentialService.GetCredentialViewModels(forceDownload);
                    base.RaisePropertyChanged("Credentials");
                });
            }
        }

        public void AddRunbook(RunbookViewModel runbook)
        {
            if (!Runbooks.Contains(runbook))
            {
                Runbooks.Add(runbook);
                base.RaisePropertyChanged("Runbooks");
            }
        }

        public void RemoveRunbook(RunbookViewModel runbook)
        {
            if (Runbooks.Contains(runbook))
            {
                Runbooks.Remove(runbook);
                base.RaisePropertyChanged("Runbooks");
            }
        }

        public void AddVariable(VariableViewModel variable)
        {
            if (!Variables.Contains(variable))
            {
                Variables.Add(variable);
                base.RaisePropertyChanged("Variables");
            }
        }

        public void RemoveVariable(VariableViewModel variable)
        {
            if (Variables.Contains(variable))
            {
                Variables.Remove(variable);
                base.RaisePropertyChanged("Variables");
            }
        }

        public ObservableCollection<RunbookViewModel> Runbooks { get; set; }

        public ObservableCollection<VariableViewModel> Variables { get; set; }

        public ObservableCollection<CredentialViewModel> Credentials { get; set; }

        public ICommand LoadCommand
        {
            get { return _loadCommand; }
        }

        public ICommand CheckInCommand
        {
            get { return _checkInCommand; }
        }

        public ICommand CheckOutCommand
        {
            get { return _checkOutCommand; }
        }

        public ICommand NewRunbookCommand
        {
            get { return _newRunbookCommand; }
        }

        public ICommand NewVariableCommand
        {
            get { return _newVariableCommand; }
        }

        public ICommand NewCredentialCommand
        {
            get { return _newCredentialCommand; }
        }

        public ICommand DeleteCommand
        {
            get { return _deleteCommand; }
        }

        /// <summary>
        /// DEPRECATED
        /// </summary>
        public IDocumentViewModel SelectedItem
        {
            get { return _selectedRunbook; }
            set { _selectedRunbook = value; }
        }
    }
}
