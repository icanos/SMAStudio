using SMAStudio.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SMAStudio.Commands
{
    public class NewRunbookCommand : ICommand
    {
        private WorkspaceViewModel _workspaceViewModel;

        public NewRunbookCommand(WorkspaceViewModel workspaceViewModel)
        {
            _workspaceViewModel = workspaceViewModel;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object parameter)
        {
            var newRunbook = new RunbookViewModel
            {
                Runbook = new SMAWebService.Runbook(),
                CheckedOut = true,
                UnsavedChanges = true
            };
            
            _workspaceViewModel.OpenDocument(newRunbook);
        }
    }
}
