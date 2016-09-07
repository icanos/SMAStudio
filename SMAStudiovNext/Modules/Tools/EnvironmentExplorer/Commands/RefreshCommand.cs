using System;
using System.Windows.Input;
using Caliburn.Micro;
using Gemini.Framework;
using SMAStudiovNext.Modules.Startup;
using SMAStudiovNext.Core;

namespace SMAStudiovNext.Modules.PartEnvironmentExplorer.Commands
{
    public class RefreshCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            var backendContextManager = IoC.Get<IBackendContextManager>();
            backendContextManager.Refresh();
        }
    }
}
