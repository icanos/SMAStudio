using SMAStudio.Services;
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
        private IVariableService _variableService;

        public NewVariableCommand()
        {
            _variableService = Core.Resolve<IVariableService>();
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
            _variableService.Create();
        }
    }
}
