using SMAStudio.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SMAStudio.Commands
{
    public class NewVariableCommand : ICommand
    {
        private WorkspaceViewModel _workspaceViewModel;

        public NewVariableCommand(WorkspaceViewModel workspaceViewModel)
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
            var newVariable = new VariableViewModel
            {
                Variable = new SMAWebService.Variable(),
                CheckedOut = true,
                UnsavedChanges = true
            };

            newVariable.Variable.Name = string.Empty;
            newVariable.Variable.Value = string.Empty;

            newVariable.Variable.VariableID = Guid.Empty;

            _workspaceViewModel.OpenDocument(newVariable);
        }
    }
}
