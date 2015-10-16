using SMAStudio.Services;
using SMAStudio.SMAWebService;
using SMAStudio.Util;
using SMAStudio.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Linq;
using System.Windows;

namespace SMAStudio.Commands
{
    public enum PrepareStatus
    {
        Cancelled,
        Run
    }

    public class RunParameter
    {
        public PrepareStatus Status { get; set; }

        public List<NameValuePair> Parameters { get; set; }
    }

    public abstract class BaseRunCommand
    {
        internal RunParameter GetUserParameters(RunbookViewModel runbook)
        {
            var window = new PrepareRunWindow(runbook);
            window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;

            if (!(bool)window.ShowDialog())
            {
                return new RunParameter
                {
                    Parameters = null,
                    Status = PrepareStatus.Cancelled
                };
            }

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
                        return GetUserParameters(runbook);
                    }

                    nameValuePair.Value = (string)value;

                    parameters.Add(nameValuePair);
                }
            }

            return new RunParameter
            {
                Parameters = parameters,
                Status = PrepareStatus.Run
            };
        }

        internal void DisplayExecutionProgress(RunbookViewModel runbook, List<NameValuePair> parameters)
        {
            var executionWindow = new ExecutionWindow(runbook);
            executionWindow.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;

            executionWindow.Show();
        }

        internal bool CheckForRunningRunbooks(RunbookViewModel runbook)
        {
            var api = Core.Resolve<IApiService>();
            var runbookService = Core.Resolve<IRunbookService>();

            Guid jobId = Guid.Empty;

            if ((jobId = runbookService.GetActiveJobs(runbook.Runbook)) != Guid.Empty)
            {
                var job = api.Current.Jobs.Where(j => j.JobID.Equals(jobId)).First();

                if (MessageBox.Show("Another job is already running for this runbook. Do you want to terminate it?", "Terminate runbook", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    try
                    {
                        job.Stop(api.Current);
                        MessageBox.Show("Stop job request has been sent. Please wait a few seconds before starting it again.", "Stop job");
                    }
                    catch (DataServiceQueryException ex)
                    {
                        Core.Log.Error("Unable to terminate the job, the job has a pending action.", ex);
                        MessageBox.Show("The job cannot be terminated since it has a pending action, please wait for it to complete.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    return false;
                }
            }

            return true;
        }
    }
}
