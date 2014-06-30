using SMAStudio.Util;
using SMAStudio.Services;
using SMAStudio.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SMAStudio.Commands
{
    public class ResumeCommand : ICommand
    {
        private IRunbookService _runbookService;

        public ResumeCommand()
        {
            _runbookService = Core.Resolve<IRunbookService>();
        }

        public bool CanExecute(object parameter)
        {
            if (!(parameter is RunbookViewModel) && !(parameter is ExecutionViewModel))
                return false;

            RunbookViewModel runbook = null;
            if (parameter is RunbookViewModel)
                runbook = (RunbookViewModel)parameter;
            else
                runbook = ((ExecutionViewModel)parameter).Runbook;

            Guid jobGuid = Guid.Empty;

            if ((jobGuid = _runbookService.GetSuspendedJobs(runbook.Runbook)) != Guid.Empty)
            {
                runbook.JobID = jobGuid;
                return true;
            }

            return false;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object parameter)
        {
            if (!(parameter is RunbookViewModel) && !(parameter is ExecutionViewModel))
                return;

            RunbookViewModel runbook = null;
            if (parameter is RunbookViewModel)
                runbook = (RunbookViewModel)parameter;
            else
                runbook = ((ExecutionViewModel)parameter).Runbook;

            var api = Core.Resolve<IApiService>();
            var job = api.Current.Jobs.Where(j => j.JobID.Equals(runbook.JobID)).FirstOrDefault();
            
            if (job == null)
            {
                Core.Log.WarningFormat("Resume Runbook: The given JobID was not found in SMA anymore. Either this job has been resumed outside of SMA Studio or there is some kind of connectivity issue. Please try again later.");
                return;
            }

            job.Resume(api.Current);

            // If we execute from the ExecutionWindow, the parameter is of type ExecutionViewModel
            // and don't want to open a new execution window.
            if (parameter is RunbookViewModel)
            {
                var executionWindow = new ExecutionWindow(runbook);
                executionWindow.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;

                executionWindow.Show();
            }
        }
    }
}
