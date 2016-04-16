using System;
using System.Windows.Input;
using Caliburn.Micro;
using Gemini.Framework.Services;
using SMAStudiovNext.Models;
using SMAStudiovNext.Modules.WindowJobHistory.ViewModels;
using SMAStudiovNext.Modules.WindowRunbook.ViewModels;
using SMAStudiovNext.Utils;

namespace SMAStudiovNext.Modules.PartEnvironmentExplorer.Commands
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
