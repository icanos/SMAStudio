using SMAStudiovNext.Modules.ConnectionManager.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SMAStudiovNext.Modules.EnvironmentExplorer.Commands
{
    public class NewConnectionCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            var connManagerWindow = new ConnectionManagerWindow();
            connManagerWindow.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            connManagerWindow.ShowDialog();
        }
    }
}
