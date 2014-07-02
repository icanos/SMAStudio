using SMAStudio.Services;
using SMAStudio.SMAWebService;
using SMAStudio.Util;
using SMAStudio.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SMAStudio.Commands
{
    public class TestCommand : BaseRunCommand, ICommand
    {
        private ApiService _api;

        public TestCommand()
        {
            _api = new ApiService();
        }

        public bool CanExecute(object parameter)
        {
            if (parameter is RunbookViewModel)
            {
                if (((RunbookViewModel)parameter).CheckedOut)
                    return true;

                return false;
            }

            if (parameter is ExecutionViewModel)
            {
                if (((ExecutionViewModel)parameter).Runbook.CheckedOut)
                    return true;

                return false;
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
            if (parameter == null)
                return;

            RunbookViewModel runbook = null;

            if (parameter is RunbookViewModel)
                runbook = (RunbookViewModel)parameter;
            else if (parameter is ExecutionViewModel)
                runbook = ((ExecutionViewModel)parameter).Runbook;

            // We need to save the runbook before testing it, this is
            // to assure that we test the latest code.
            var saveCommand = Core.Resolve<ICommand>("Save");
            if (saveCommand != null)
            {
                saveCommand.Execute(runbook);
            }
            else
            {
                Core.Log.ErrorFormat("No SaveCommand was found. This can't happen?");
            }

            // Check if the runbook is already running
            if (!CheckForRunningRunbooks(runbook))
            {
                Core.Log.DebugFormat("User cancelled execution because of running job.");
                return;
            }

            // Retrieve any parameters and their input values from the user
            var parameters = GetUserParameters(runbook);

            Guid? jobGuid = new Guid?(runbook.Runbook.TestRunbook(_api.Current, parameters));
            runbook.JobID = (Guid)jobGuid;

            // Display execution progress
            DisplayExecutionProgress(runbook, parameters);
        }
    }
}
