using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SMAStudio.Commands
{
    public class CompareCommand : ICommand
    {
        private CompareWindow _compareWindow;

        public CompareCommand(CompareWindow compareWindow)
        {
            _compareWindow = compareWindow;
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
            RevisionsWindow window = new RevisionsWindow(_compareWindow.Runbook);
            if ((bool)window.ShowDialog())
            {
                // Reload the compare window
                _compareWindow.Version = window.SelectedVersion;
            }
        }
    }
}
