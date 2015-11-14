using System;
using System.Windows.Input;

namespace SMAStudiovNext.Commands
{
    public class RelayCommand : ICommand
    {
        private readonly Func<bool> _canExecutionAction;
        private readonly Action _executeAction;

        public RelayCommand(Func<bool> canExecuteAction, Action executeAction)
        {
            _canExecutionAction = canExecuteAction;
            _executeAction = executeAction;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return _canExecutionAction();
        }

        public void Execute(object parameter)
        {
            _executeAction();
        }
    }
}
