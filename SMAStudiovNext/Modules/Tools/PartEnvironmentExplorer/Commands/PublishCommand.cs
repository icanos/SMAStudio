using System;
using System.Windows.Input;
using SMAStudiovNext.Models;
using SMAStudiovNext.Modules.WindowRunbook.ViewModels;
using SMAStudiovNext.Utils;

namespace SMAStudiovNext.Modules.PartEnvironmentExplorer.Commands
{
    public class PublishCommand : ICommand
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

            if (!(item.Tag is RunbookModelProxy))
                return false;

            if ((item.Tag as RunbookModelProxy).DraftRunbookVersionID.HasValue)
                return true;

            return false;
        }

        public async void Execute(object parameter)
        {
            var item = (ResourceContainer)parameter;
            var runbook = (RunbookModelProxy)item.Tag;

            var viewModel = (item.Tag as RunbookModelProxy).GetViewModel<RunbookViewModel>();
            await viewModel.CheckIn();
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
