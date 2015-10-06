using SMAStudio.Util;
using SMAStudio.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SMAStudio.Commands
{
    public class StopCommand : ICommand
    {
        private ApiService _api;

        public StopCommand()
        {
            _api = new ApiService();
        }

        public bool CanExecute(object parameter)
        {
            if (parameter == null)
                return false;

            if (!(parameter is RunbookViewModel) && !(parameter is ExecutionViewModel))
                return false;

            bool result = true;

            if (parameter is RunbookViewModel)
                result = !(((RunbookViewModel)parameter).JobID == Guid.Empty);
            else if (parameter is ExecutionViewModel)
                result = !(((ExecutionViewModel)parameter).Runbook.JobID == Guid.Empty);

            //if (((RunbookViewModel)parameter).JobID == Guid.Empty)
            //    return false;

            return result;
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

            if (MessageBox.Show("Are you sure you want to stop the execution?", "Stop execution", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                RunbookViewModel runbook = null;

                if (parameter is RunbookViewModel)
                    runbook = (RunbookViewModel)parameter;
                else if (parameter is ExecutionViewModel)
                    runbook = ((ExecutionViewModel)parameter).Runbook;
                else
                    throw new Exception("Invalid object");

                var job = _api.Current.Jobs.Where(j => j.JobID == runbook.JobID).First();

                try
                {
                    job.Stop(_api.Current);
                }
                catch (DataServiceQueryException ex)
                {
                    Core.Log.Error(ex.Message, ex);
                    MessageBox.Show("Unable to stop the job since it already have a pending action.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                runbook.JobID = Guid.Empty;
            }
        }
    }
}
