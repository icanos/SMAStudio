using SMAStudio.Services;
using SMAStudio.Util;
using SMAStudio.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SMAStudio.Commands
{
    public class DeleteCommand : ICommand
    {
        private IApiService _api;
        private IRunbookService _runbookService;
        private IVariableService _variableService;
        private ICredentialService _credentialService;

        public DeleteCommand()
        {
            _api = Core.Resolve<IApiService>();
            _runbookService = Core.Resolve<IRunbookService>();
            _variableService = Core.Resolve<IVariableService>();
            _credentialService = Core.Resolve<ICredentialService>();
        }

        public bool CanExecute(object parameter)
        {
            if (!(parameter is IDocumentViewModel))
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
            if (MessageBox.Show("Are you sure you want to delete this item?", "Delete", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            if (parameter is RunbookViewModel)
                DeleteRunbook((RunbookViewModel)parameter);
            else if (parameter is VariableViewModel)
                DeleteVariable((VariableViewModel)parameter);
            else if (parameter is CredentialViewModel)
                DeleteCredential((CredentialViewModel)parameter);

            // Reload the left side menu as well
            var componentsViewModel = Core.Resolve<IComponentsViewModel>();
            componentsViewModel.Load(true /* force download */);
        }

        private void DeleteRunbook(RunbookViewModel runbookViewModel)
        {
            _runbookService.Delete(runbookViewModel);
        }

        private void DeleteVariable(VariableViewModel variableViewModel)
        {
            _variableService.Delete(variableViewModel);
        }

        private void DeleteCredential(CredentialViewModel credentialViewModel)
        {
            _credentialService.Delete(credentialViewModel);
        }
    }
}
