using SMAStudio.Models;
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
    public class GoDefinitionCommand : ICommand
    {
        private IWorkspaceViewModel _workspaceViewModel;
        private IComponentsViewModel _componentsViewModel;

        public GoDefinitionCommand()
        {
            _workspaceViewModel = Core.Resolve<IWorkspaceViewModel>();
            _componentsViewModel = Core.Resolve<IComponentsViewModel>();
        }

        public bool CanExecute(object parameter)
        {
            if (parameter is DocumentReference)
                return true;

            return false;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object parameter)
        {
            RunbookViewModel document = null;
            var reference = (DocumentReference)parameter;

            document = _componentsViewModel.Runbooks.Where(r => r.RunbookName.Equals(reference.Destination, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();

            if (document == null)
            {
                MessageBox.Show("No runbook named '" + reference.Destination + "' exists in this SMA environment.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (_workspaceViewModel.Documents.Contains(document))
            {
                int pos = _workspaceViewModel.Documents.IndexOf(document);

                if (pos > 0 && pos < _workspaceViewModel.Documents.Count)
                    _workspaceViewModel.SelectedIndex = pos;
            }
            else
            {
                _workspaceViewModel.OpenDocument(document);
            }
        }
    }
}
