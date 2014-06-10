using SMAStudio.Util;
using SMAStudio.SMAWebService;
using SMAStudio.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using SMAStudio.Logging;

namespace SMAStudio.Commands
{
    public class CheckInCommand : ICommand
    {
        private ApiService _api;
        private ILoggingService _log;

        public CheckInCommand()
        {
            _api = new ApiService();
            _log = new log4netLoggingService();
        }

        public bool CanExecute(object parameter)
        {
            if (parameter == null)
                return false;

            if (!(parameter is RunbookViewModel))
                return false;
            
            var document = ((IDocumentViewModel)parameter);

            // If this is a new runbook, it won't be able to be checked in before
            // it has been saved.
            if (String.IsNullOrEmpty(((RunbookViewModel)document).RunbookName))
                return false;

            if (!document.CheckedOut)
                return false;

            return true;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object parameter)
        {
            if (parameter == null)
                return;

            var rb = ((RunbookViewModel)parameter);

            var runbook = _api.Current.Runbooks.Where(r => r.RunbookID == rb.Runbook.RunbookID).FirstOrDefault();
            if (runbook == null)
            {
                MessageBox.Show("The runbook does not exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!runbook.DraftRunbookVersionID.HasValue || runbook.DraftRunbookVersionID == Guid.Empty)
            {
                MessageBox.Show("The runbook's already checked in.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                // Publish the runbook
                runbook.Publish(_api.Current);

                rb.CheckedOut = false;
                rb.Runbook = runbook;
            }
            catch (Exception e)
            {
                _log.Error("Something went wrong when checking in the runbook.", e);
                MessageBox.Show("Something went wrong when trying to check in the runbook. Refer to the logs for more information.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }
    }
}
