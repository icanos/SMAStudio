using SMAStudio.Util;
using SMAStudio.SMAWebService;
using SMAStudio.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Data.Services.Client;
using SMAStudio.Services;

namespace SMAStudio.Commands
{
    public class SaveCommand : ICommand
    {
        private IApiService _api;
        private IRunbookService _runbookService;
        private IVariableService _variableService;
        private ICredentialService _credentialService;

        public SaveCommand()
        {
            _api = Core.Resolve<IApiService>();
            _runbookService = Core.Resolve<IRunbookService>();
            _variableService = Core.Resolve<IVariableService>();
            _credentialService = Core.Resolve<ICredentialService>();
        }

        public bool CanExecute(object parameter)
        {
            if (parameter == null)
                return false;

            var document = ((IDocumentViewModel)parameter);

            if (!document.UnsavedChanges)
                return false;

            return true;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object parameter)
        {
            if (parameter == null)
                return;

            if (parameter is RunbookViewModel)
                SaveRunbook((RunbookViewModel)parameter);
            else if (parameter is VariableViewModel)
                SaveVariable((VariableViewModel)parameter);
            else if (parameter is CredentialViewModel)
                SaveCredential((CredentialViewModel)parameter);
        }

        private void SaveRunbook(RunbookViewModel rb)
        {
            _runbookService.Update(rb);
        }

        private void SaveVariable(VariableViewModel variable)
        {
            _variableService.Update(variable);
        }

        private void SaveCredential(CredentialViewModel credential)
        {
            _credentialService.Update(credential);
        }
    }
}
