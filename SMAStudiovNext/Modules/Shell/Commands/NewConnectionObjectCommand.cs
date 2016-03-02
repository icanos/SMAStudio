using Caliburn.Micro;
using Gemini.Framework.Services;
using SMAStudiovNext.Models;
using SMAStudiovNext.Modules.EnvironmentExplorer.ViewModels;
using SMAStudiovNext.Modules.WindowConnection.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SMAStudiovNext.Modules.Shell.Commands
{
    public class NewConnectionObjectCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            var shell = IoC.Get<IShell>();

            var context = IoC.Get<EnvironmentExplorerViewModel>().GetCurrentContext();
            var viewModel = new ConnectionViewModel(new ConnectionTypeModelProxy(new SMA.ConnectionType(), context));

            shell.OpenDocument(viewModel);
        }
    }
}
