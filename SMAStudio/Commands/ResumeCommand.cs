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
            if (!(parameter is RunbookViewModel))
                return false;

            var runbook = (RunbookViewModel)parameter;
            Guid jobGuid = Guid.Empty;

            if ((jobGuid = _runbookService.GetSuspendedJobs(runbook)) != Guid.Empty)
            {
                runbook.JobID = jobGuid;
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
            // TODO: Implement resuming
        }
    }
}
