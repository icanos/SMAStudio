using Caliburn.Micro;
using Gemini.Framework.Services;
using SMAStudiovNext.Models;
using System;
using System.Windows;
using System.Windows.Input;
using SMAStudiovNext.Modules.PartEnvironmentExplorer.ViewModels;
using SMAStudiovNext.Modules.WindowVariable.ViewModels;
using SMAStudiovNext.Services;

namespace SMAStudiovNext.Modules.Shell.Commands
{
    public class NewVariableCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            var shell = IoC.Get<IShell>();
            //shell.OpenDocument(new VariableViewModel(new VariableModelProxy(new SMA.Variable())));
            //MessageBox.Show("Reimplement this with support for both Azure and SMA!");

            var context = IoC.Get<EnvironmentExplorerViewModel>().GetCurrentContext();
            var viewModel = default(VariableViewModel);
            
            if (context.Service is AzureService || context.Service is AzureRMService)
                viewModel = new VariableViewModel(new VariableModelProxy(new Vendor.Azure.Variable(), context));
            else
                viewModel = new VariableViewModel(new VariableModelProxy(new SMA.Variable(), context));

            shell.OpenDocument(viewModel);
        }
    }
}
