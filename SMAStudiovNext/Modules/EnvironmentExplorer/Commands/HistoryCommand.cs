using Caliburn.Micro;
using Gemini.Framework.Services;
using SMAStudiovNext.Core;
using SMAStudiovNext.Models;
using SMAStudiovNext.Modules.EnvironmentExplorer.ViewModels;
using SMAStudiovNext.Modules.JobHistory.ViewModels;
using SMAStudiovNext.Modules.Runbook.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SMAStudiovNext.Modules.EnvironmentExplorer.Commands
{
    public class HistoryCommand : ICommand
    {
        private EventHandler _internalCanExecuteChanged;
        public event EventHandler CanExecuteChanged
        {
            add
            {
                _internalCanExecuteChanged += value;
                CommandManager.RequerySuggested += value;
            }
            remove
            {
                _internalCanExecuteChanged -= value;
                CommandManager.RequerySuggested -= value;
            }
        }

        public bool CanExecute(object parameter)
        {
            var item = (ResourceContainer)parameter;

            if (item == null)
                return false;

            if (item.Tag is RunbookModelProxy)
                return true;

            return false;
        }

        public void Execute(object parameter)
        {
            var item = (ResourceContainer)parameter;
            var runbook = (RunbookModelProxy)item.Tag;

            var shell = IoC.Get<IShell>();
            shell.OpenDocument(new JobHistoryViewModel(runbook.GetViewModel<RunbookViewModel>()));
        }

        /// <summary>
        /// This method is used to walk the delegate chain and well WPF that
        /// our command execution status has changed.
        /// </summary>
        protected virtual void OnCanExecuteChanged()
        {
            EventHandler eCanExecuteChanged = _internalCanExecuteChanged;

            if (eCanExecuteChanged != null)
                eCanExecuteChanged(this, EventArgs.Empty);
        }
    }
}
