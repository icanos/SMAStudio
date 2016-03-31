using Caliburn.Micro;
using Gemini.Framework.Services;
using SMAStudiovNext.Models;
using SMAStudiovNext.Modules.WindowModule.ViewModels;
using SMAStudiovNext.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using SMAStudiovNext.Modules.PartEnvironmentExplorer.ViewModels;

namespace SMAStudiovNext.Modules.Shell.Commands
{
    public class NewModuleCommand : ICommand
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
            var viewModel = default(ModuleViewModel);

            if (context.Service is AzureService)
                viewModel = new ModuleViewModel(new ModuleModelProxy(new Vendor.Azure.Module(), context));
            else
                viewModel = new ModuleViewModel(new ModuleModelProxy(new SMA.Module(), context));

            shell.OpenDocument(viewModel);
        }
    }
}
