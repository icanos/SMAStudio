using Caliburn.Micro;
using Gemini.Framework.Services;
using SMAStudiovNext.Models;
using SMAStudiovNext.Modules.Credential.ViewModels;
using SMAStudiovNext.Modules.EnvironmentExplorer.ViewModels;
using System;
using System.Windows;
using System.Windows.Input;

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
