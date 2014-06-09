using SMAStudio.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SMAStudio.Commands
{
    public class CloseCommand : ICommand
    {
        private WorkspaceViewModel _workspaceViewModel;

        public CloseCommand(WorkspaceViewModel workspaceViewModel)
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
            if (parameter == null)
                return;

            var document = (IDocumentViewModel)parameter;

            // In case we're closing the currently selected tab - we need to change the index
            // in order for our tabcontrol not to flip...
            int idx = _workspaceViewModel.Documents.IndexOf(document);

            if (_workspaceViewModel.SelectedIndex == idx)
                _workspaceViewModel.SelectedIndex = (idx > 0) ? idx-- : 0;

            _workspaceViewModel.Documents.Remove(document);
        }
    }
}
