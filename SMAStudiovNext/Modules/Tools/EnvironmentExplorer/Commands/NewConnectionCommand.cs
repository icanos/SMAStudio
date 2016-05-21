using System;
using System.Windows.Input;
using SMAStudiovNext.Modules.DialogConnectionManager.Windows;

namespace SMAStudiovNext.Modules.PartEnvironmentExplorer.Commands
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
