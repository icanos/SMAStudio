using System;
using System.Windows;
using System.Windows.Input;
using Caliburn.Micro;
using SMAStudiovNext.Exceptions;
using SMAStudiovNext.Models;
using SMAStudiovNext.Modules.PartEnvironmentExplorer.ViewModels;
using SMAStudiovNext.Utils;
using SMAStudiovNext.Core;

namespace SMAStudiovNext.Modules.PartEnvironmentExplorer.Commands
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
                LongRunningOperation.Start();
                var item = (ModelProxyBase)(parameter as ResourceContainer).Tag;

                // Make sure that we remove the object from the context as well
                if (item is RunbookModelProxy)
                    backendService.Context.Runbooks.Remove(parameter as ResourceContainer);
                else if (item is ConnectionModelProxy)
                    backendService.Context.Connections.Remove(parameter as ResourceContainer);
                else if (item is ScheduleModelProxy)
                    backendService.Context.Schedules.Remove(parameter as ResourceContainer);
                else if (item is VariableModelProxy)
                    backendService.Context.Variables.Remove(parameter as ResourceContainer);
                else if (item is ModuleModelProxy)
                    backendService.Context.Modules.Remove(parameter as ResourceContainer);
                else if (item is CredentialModelProxy)
                    backendService.Context.Credentials.Remove(parameter as ResourceContainer);

                try {
                    if (backendService.Delete(item))
                        environmentExplorer.Delete(parameter as ResourceContainer);
                }
                catch (ApplicationException ex)
                {
                    GlobalExceptionHandler.Show(ex);
                }

                LongRunningOperation.Stop();
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
