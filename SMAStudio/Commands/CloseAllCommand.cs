using SMAStudio.Util;
using SMAStudio.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SMAStudio.Commands
{
    public class CloseAllCommand : ICommand
    {
        private WorkspaceViewModel _workspaceViewModel;

        public CloseAllCommand(WorkspaceViewModel workspaceViewModel)
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
            var documents = _workspaceViewModel.Documents.Clone();

            foreach (var item in documents)
            {
                if (!item.Equals(document))
                    _workspaceViewModel.Documents.Remove(item);
            }

            documents = null;
        }
    }
}
