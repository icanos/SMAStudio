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
        private ApiService _api;
        private ComponentsViewModel _componentsViewModel;
        private WorkspaceViewModel _workspaceViewModel;

        public DeleteCommand(ComponentsViewModel componentsViewModel, WorkspaceViewModel workspaceViewModel)
        {
            _api = new ApiService();
            _componentsViewModel = componentsViewModel;
            _workspaceViewModel = workspaceViewModel;
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
        }

        private void DeleteRunbook(RunbookViewModel runbookViewModel)
        {
            var runbook = _api.Current.Runbooks.Where(r => r.RunbookID == runbookViewModel.Runbook.RunbookID).FirstOrDefault();

            if (runbook == null)
            {
                Core.Log.DebugFormat("Trying to remove a runbook that doesn't exist. GUID: {0}", runbookViewModel.Runbook.RunbookID);
                return;
            }

            _api.Current.DeleteObject(runbook);
            _api.Current.SaveChanges();

            // Remove the runbook from the list of runbooks
            _componentsViewModel.RemoveRunbook(runbookViewModel);

            // If the runbook is open, we close it
            if (_workspaceViewModel.Documents.Contains(runbookViewModel))
                _workspaceViewModel.Documents.Remove(runbookViewModel);
        }

        private void DeleteVariable(VariableViewModel variableViewModel)
        {
            var variable = _api.Current.Variables.Where(v => v.VariableID == variableViewModel.ID).FirstOrDefault();

            if (variable == null)
            {
                Core.Log.DebugFormat("Trying to remove a variable that doesn't exist. GUID {0}", variableViewModel.ID);
                return;
            }

            _api.Current.DeleteObject(variable);
            _api.Current.SaveChanges();

            // Remove the variable from the list of variables
            _componentsViewModel.RemoveVariable(variableViewModel);

            // If the variable is open, we close it
            if (_workspaceViewModel.Documents.Contains(variableViewModel))
                _workspaceViewModel.Documents.Remove(variableViewModel);
        }

        private void DeleteCredential(CredentialViewModel credentialViewModel)
        {
            var credential = _api.Current.Credentials.Where(c => c.CredentialID == credentialViewModel.ID).FirstOrDefault();

            if (credential == null)
            {
                Core.Log.DebugFormat("Trying to remove a credential that doesn't exist. GUID {0}", credentialViewModel.ID);
                return;
            }

            _api.Current.DeleteObject(credential);
            _api.Current.SaveChanges();

            _componentsViewModel.Credentials.Remove(credentialViewModel);

            if (_workspaceViewModel.Documents.Contains(credentialViewModel))
                _workspaceViewModel.Documents.Remove(credentialViewModel);
        }
    }
}
