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
    public class RunCommand : ICommand
    {
        private ApiService _api;

        public RunCommand()
        {
            _api = new ApiService();
        }

        public bool CanExecute(object parameter)
        {
            if (parameter is RunbookViewModel)
            {
                if (((RunbookViewModel)parameter).CheckedOut)
                    return false;

                return true;
            }

            if (parameter is ExecutionViewModel)
            {
                if (((ExecutionViewModel)parameter).Runbook.CheckedOut)
                    return false;

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
                    var nameValuePair = new NameValuePair
                        {
                            Name = param.Command,
                        };

                    // Parse the value to the correct data type and convert to json
                    var value = TypeConverter.Convert(param);

                    if (value == null)
                    {
                        MessageBox.Show(String.Format("Invalid data type for parameter '{0}'. Expected data type was: {1}", param.Name, param.TypeName), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    nameValuePair.Value = (string)value;

                    parameters.Add(nameValuePair);
                }
            }

            try
            {
                _api.Current.AttachTo("Runbooks", runbook.Runbook);
                _api.Current.UpdateObject(runbook.Runbook);
            }
            catch (InvalidOperationException)
            {

            }

            var runbookService = Core.Resolve<IRunbookService>();
            Guid jobId = Guid.Empty;

            if ((jobId = runbookService.GetSuspendedJobs(runbook.Runbook)) != Guid.Empty)
            {
                var job = _api.Current.Jobs.Where(j => j.JobID.Equals(jobId)).First();

                if (MessageBox.Show("Another job is already running for this runbook. Do you want to terminate it?", "Terminate runbook", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    job.Stop(_api.Current);
                    MessageBox.Show("Stop job request has been sent. Please wait a few seconds before starting it again.", "Stop job");
                    return;
                }
            }

            Guid? jobGuid = new Guid?(runbook.Runbook.StartRunbook(_api.Current, parameters));
            runbook.JobID = (Guid)jobGuid;
            
            var executionWindow = new ExecutionWindow(runbook);
            executionWindow.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;

            executionWindow.Show();
        }
    }
}
