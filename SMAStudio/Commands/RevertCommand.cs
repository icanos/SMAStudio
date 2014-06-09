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
    public class RevertCommand : ICommand
    {
        public bool CanExecute(object parameter)
        {
            if (parameter == null)
                return false;

            if (!(parameter is RunbookViewModel))
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
            if (parameter == null)
                return;

            var runbook = (RunbookViewModel)parameter;

            RevisionsWindow window = new RevisionsWindow(runbook);
            if ((bool)window.ShowDialog())
            {
                // Revert to this runbook
                MessageBox.Show("This feature is not yet implemented.");
            }
        }
    }
}
