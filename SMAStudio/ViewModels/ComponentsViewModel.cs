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
    public class ComponentsViewModel : ObservableObject, IComponentsViewModel
    {
        private IWorkspaceViewModel _dataContext;

        private IRunbookService _runbookService;
        private IVariableService _variableService;
        private ICredentialService _credentialService;

        private object _sync = new object();
        
        public ComponentsViewModel(IWorkspaceViewModel dataContext)
        {
            _dataContext = dataContext;

            Runbooks = new ObservableCollection<RunbookViewModel>();
            Variables = new ObservableCollection<VariableViewModel>();
            Credentials = new ObservableCollection<CredentialViewModel>();
        }

        public void Initialize()
        {
            _runbookService = Core.Resolve<IRunbookService>();
            _variableService = Core.Resolve<IVariableService>();
            _credentialService = Core.Resolve<ICredentialService>();

            Load();
        }

        /// <summary>
        /// Called when the application is launched to load runbooks, variables and credentials
        /// from the SMA web service.
        /// </summary>
        /// <param name="forceDownload">Set to true to force the API manager to download new content instead of using potentially cached data.</param>
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

            if (((BaseService)_runbookService).SuccessfulInitialization)
            {
                AsyncService.Execute(ThreadPriority.Normal, delegate()
                {
                    Core.Log.DebugFormat("Loading variables...");

                    Variables = _variableService.GetVariableViewModels(forceDownload);
                    base.RaisePropertyChanged("Variables");
                });
            }

            if (((BaseService)_runbookService).SuccessfulInitialization || ((BaseService)_variableService).SuccessfulInitialization)
            {
                AsyncService.Execute(ThreadPriority.Normal, delegate()
                {
                    Core.Log.DebugFormat("Loading credentials...");

                    Credentials = _credentialService.GetCredentialViewModels(forceDownload);
                    base.RaisePropertyChanged("Credentials");
                });
            }
        }

        /// <summary>
        /// Add a runbook to the list if it doesn't already exist
        /// </summary>
        /// <param name="runbook"></param>
        public void AddRunbook(RunbookViewModel runbook)
        {
            if (!Runbooks.Contains(runbook))
            {
                Runbooks.Add(runbook);
                base.RaisePropertyChanged("Runbooks");
            }
        }

        /// <summary>
        /// Remove a runbook from the list if it exist
        /// </summary>
        /// <param name="runbook"></param>
        public void RemoveRunbook(RunbookViewModel runbook)
        {
            if (Runbooks.Contains(runbook))
            {
                Runbooks.Remove(runbook);
                base.RaisePropertyChanged("Runbooks");
            }
        }

        /// <summary>
        /// Add a variable to the list if it doesn't already exist
        /// </summary>
        /// <param name="variable"></param>
        public void AddVariable(VariableViewModel variable)
        {
            if (!Variables.Contains(variable))
            {
                Variables.Add(variable);
                base.RaisePropertyChanged("Variables");
            }
        }

        /// <summary>
        /// Remove a variable from the list if it exist
        /// </summary>
        /// <param name="variable"></param>
        public void RemoveVariable(VariableViewModel variable)
        {
            if (Variables.Contains(variable))
            {
                Variables.Remove(variable);
                base.RaisePropertyChanged("Variables");
            }
        }

        #region Properties
        public ObservableCollection<RunbookViewModel> Runbooks { get; set; }

        public ObservableCollection<VariableViewModel> Variables { get; set; }

        public ObservableCollection<CredentialViewModel> Credentials { get; set; }

        public ICommand LoadCommand
        {
            get { return Core.Resolve<ICommand>("Load"); }
        }

        public ICommand CheckInCommand
        {
            get { return Core.Resolve<ICommand>("CheckIn"); }
        }

        public ICommand CheckOutCommand
        {
            get { return Core.Resolve<ICommand>("CheckOut"); }
        }

        public ICommand NewRunbookCommand
        {
            get { return Core.Resolve<ICommand>("NewRunbook"); }
        }

        public ICommand NewVariableCommand
        {
            get { return Core.Resolve<ICommand>("NewVariable"); }
        }

        public ICommand NewCredentialCommand
        {
            get { return Core.Resolve<ICommand>("NewCredential"); }
        }

        public ICommand DeleteCommand
        {
            get { return Core.Resolve<ICommand>("Delete"); }
        }
        #endregion
    }
}
