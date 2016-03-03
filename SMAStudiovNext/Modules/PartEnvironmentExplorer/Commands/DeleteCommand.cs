using Caliburn.Micro;
using SMAStudiovNext.Core;
using SMAStudiovNext.Models;
using SMAStudiovNext.Modules.EnvironmentExplorer.ViewModels;
using SMAStudiovNext.Services;
using System;
using System.Windows;
using System.Windows.Input;

namespace SMAStudiovNext.Modules.EnvironmentExplorer.Commands
{
    public class DeleteCommand : ICommand
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

            if (item.Tag == null)
                return false;

            if (item.Tag is RunbookModelProxy || item.Tag is VariableModelProxy || item.Tag is CredentialModelProxy || item.Tag is ScheduleModelProxy || item.Tag is ModuleModelProxy || item.Tag is ConnectionModelProxy)
                return true;

            return false;
        }

        public void Execute(object parameter)
        {
            if (parameter == null)
                return;

            if (!(parameter is ResourceContainer))
                return;

            if (((ResourceContainer)parameter).Tag == null)
                return;

            var backendService = ((parameter as ResourceContainer).Tag as ModelProxyBase).Context.Service;
            var environmentExplorer = IoC.Get<EnvironmentExplorerViewModel>();

            if (MessageBox.Show("Are you sure you want to delete the object? This cannot be reverted.", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                var item = (ModelProxyBase)(parameter as ResourceContainer).Tag;
                
                if (backendService.Delete(item))
                    environmentExplorer.Delete(parameter as ResourceContainer);
            }
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
