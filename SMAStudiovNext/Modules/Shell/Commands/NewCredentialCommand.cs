using Caliburn.Micro;
using Gemini.Framework.Services;
using SMAStudiovNext.Models;
using System;
using System.Windows;
using System.Windows.Input;
using SMAStudiovNext.Modules.PartEnvironmentExplorer.ViewModels;
using SMAStudiovNext.Modules.WindowCredential.ViewModels;

namespace SMAStudiovNext.Modules.Shell.Commands
{
    public class NewCredentialCommand : ICommand
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
            var viewModel = new CredentialViewModel(new CredentialModelProxy(new SMA.Credential(), context));

            shell.OpenDocument(viewModel);
        }
    }
}
