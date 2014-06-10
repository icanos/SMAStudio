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
    public class TestCommand : ICommand
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
            /*if (!(parameter is RunbookViewModel))
                return;

            var runbook = (RunbookViewModel)parameter;*/
            if (parameter is RunbookViewModel)
                runbook = (RunbookViewModel)parameter;
            else if (parameter is ExecutionViewModel)
                runbook = ((ExecutionViewModel)parameter).Runbook;

            var window = new PrepareRunWindow(runbook);
            window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;

            if (!(bool)window.ShowDialog())
                return;

            List<NameValuePair> parameters = null;

            if (window.Inputs.Count > 0)
            {
                parameters = new List<NameValuePair>();

                foreach (var param in window.Inputs)
                {
                    parameters.Add(new NameValuePair
                        {
                            Name = param.Command,
                            Value = JsonConverter.ToJson(param.Value)
                        });
                }
            }

            //if (parameter is RunbookViewModel)
            try
            {
                _api.Current.AttachTo("Runbooks", runbook.Runbook);
                _api.Current.UpdateObject(runbook.Runbook);
            }
            catch (InvalidOperationException)
            {

            }

            var jobs = _api.Current.Jobs.Where(j => !j.JobStatus.Equals("Completed") && !j.JobStatus.Equals("Failed") && !j.JobStatus.Equals("Stopped")).ToList();

            foreach (var job in jobs)
            {
                var jobContext = _api.Current.JobContexts.Where(jc => jc.RunbookVersionID.Equals(runbook.Runbook.DraftRunbookVersionID)).FirstOrDefault();

                if (jobContext == null)
                    continue;

                if (MessageBox.Show("Another job is already running for this runbook. Do you want to terminate it?", "Terminate runbook", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    job.Stop(_api.Current);
                    MessageBox.Show("Stop job request has been sent. Please wait a few seconds before starting it again.", "Stop job");
                    return;
                }
            }
            
            Guid? jobGuid = new Guid?(runbook.Runbook.TestRunbook(_api.Current, parameters));
            runbook.JobID = (Guid)jobGuid;
            
            var executionWindow = new ExecutionWindow(runbook);
            executionWindow.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;

            executionWindow.Show();
        }
    }
}
