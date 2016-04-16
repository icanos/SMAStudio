using System;
using System.Windows.Input;
using SMAStudiovNext.Core;
using SMAStudiovNext.Modules.DialogDocumentation.Windows;
using SMAStudiovNext.Utils;

namespace SMAStudiovNext.Modules.PartEnvironmentExplorer.Commands
{
    public class DocumentationCommand : ICommand
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

            if (item.Tag is BackendContext)
                return true;

            return true;
        }

        public void Execute(object parameter)
        {
            var dialog = new DocumentationWindow(((parameter as ResourceContainer).Context as IBackendContext));
            dialog.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;

            dialog.ShowDialog();
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
