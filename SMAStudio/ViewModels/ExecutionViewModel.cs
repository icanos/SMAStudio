using SMAStudio.Commands;
using SMAStudio.Services;
using SMAStudio.SMAWebService;
using SMAStudio.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml;
using System.Xml.Linq;

namespace SMAStudio.ViewModels
{
    public class ExecutionViewModel : ObservableObject, IDisposable
    {
        private ApiService _api;
        private RunbookViewModel _runbookViewModel;

        private ExecutionProperty paramStartTime = null;
        private ExecutionProperty paramEndTime = null;
        private ExecutionProperty paramErrorCount = null;
        private ExecutionProperty paramWarnCount = null;
        private ExecutionProperty paramJobStatus = null;

        private ICommand _runCommand;

        private Thread _thread = null;

        public ExecutionViewModel(RunbookViewModel runbookViewModel)
        {
            _runbookViewModel = runbookViewModel;

            _api = new ApiService();
            ExecutionProperties = new ObservableCollection<ExecutionProperty>();

            _runCommand = new RunCommand();

            // Start the retrieving of data from the webservice
            Run();
        }

        public void Run()
        {
            var doneStatuses = new List<string> { "Completed", "Failed" };

            //AsyncService.Execute(ThreadPriority.Normal, delegate()
            _thread = new Thread(new ThreadStart(delegate()
            {
                var job = GetJobDetails(_runbookViewModel.JobID);

                //var job = _api.Current.Jobs.Where(j => j.JobID == _runbookViewModel.JobID).FirstOrDefault();

                if (job == null)
                    return;

                // Add the JobID parameter
                var param1 = new ExecutionProperty("Job ID", job.JobID);
                //var param2 = new ExecutionProperty("Runbook ID", runbookVersion.RunbookID);
                //var param3 = new ExecutionProperty("Runbook Name", runbook.RunbookName);
                paramJobStatus = new ExecutionProperty("Job Status", job.JobStatus);
                paramStartTime = new ExecutionProperty("Start Time", job.StartTime);
                paramEndTime = new ExecutionProperty("End Time", job.EndTime);
                var param5 = new ExecutionProperty("Creation Time", job.CreationTime);
                var param6 = new ExecutionProperty("Last Modified Time", job.LastModifiedTime);
                paramErrorCount = new ExecutionProperty("Error Count", job.ErrorCount);
                paramWarnCount = new ExecutionProperty("Warning Count", job.WarningCount);

                App.Current.Dispatcher.Invoke((Action)delegate()
                {
                    ExecutionProperties.Add(param1);
                    //ExecutionProperties.Add(param2);
                    //ExecutionProperties.Add(param3);
                    ExecutionProperties.Add(paramJobStatus);
                    ExecutionProperties.Add(paramStartTime);
                    ExecutionProperties.Add(paramEndTime);
                    ExecutionProperties.Add(param5);
                    ExecutionProperties.Add(param6);
                    ExecutionProperties.Add(paramErrorCount);
                    ExecutionProperties.Add(paramWarnCount);
                });

                while (!doneStatuses.Contains(job.JobStatus))
                {
                    UpdateExecution(job);

                    Thread.Sleep(2 * 1000);
                    job = GetJobDetails(_runbookViewModel.JobID);
                    //job = null;
                    //job = _api.Current.Jobs.Where(j => j.JobID == _runbookViewModel.JobID).First();
                }

                UpdateExecution(job);

                Console.WriteLine("Execution is complete.");
                base.RaisePropertyChanged("ExecutionProperties");
            }));

            _thread.Start();
        }

        private void UpdateExecution(Job job)
        {
            if (App.Current == null)
            {
                _thread.Abort();
            }

            App.Current.Dispatcher.Invoke((Action)delegate()
            {
                var apiJob = _api.Current.Jobs.Where(j => j.JobID == _runbookViewModel.JobID).First();
                ExecutionContent = ApiHelpers.GetJobOutput(_api.Current, apiJob);

                paramStartTime.Value = job.StartTime;
                paramEndTime.Value = job.EndTime;
                paramErrorCount.Value = job.ErrorCount;
                paramWarnCount.Value = job.WarningCount;
                paramJobStatus.Value = job.JobStatus;

                base.RaisePropertyChanged("ExecutionContent");
                base.RaisePropertyChanged("ExecutionProperties");
            });
        }
        
        private Job GetJobDetails(Guid jobGuid)
        {
            string url = "https://wwin14.westin.local:9090/00000000-0000-0000-0000-000000000000/Jobs(guid'" + jobGuid + "')";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Credentials = CredentialCache.DefaultCredentials;
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            TextReader tr = new StreamReader(response.GetResponseStream());
            string content = tr.ReadToEnd();

            tr.Close();

            var job = new Job();

            XElement jobXml = XElement.Parse(content);
            XNamespace d = "http://schemas.microsoft.com/ado/2007/08/dataservices";
            XNamespace m = "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata";
            IEnumerable<XElement> properties = jobXml.Element(m + "properties").Elements();

            foreach (var prop in properties)
            {
                switch (prop.Name.LocalName)
                {
                    case "CreationTime":
                        job.CreationTime = DateTime.Parse(prop.Value);
                        break;
                    case "EndTime":
                        job.EndTime = !String.IsNullOrEmpty(prop.Value) ? DateTime.Parse(prop.Value) : DateTime.MinValue;
                        break;
                    case "ErrorCount":
                        job.ErrorCount = short.Parse(prop.Value);
                        break;
                    case "JobContextID":
                        job.JobContextID = Guid.Parse(prop.Value);
                        break;
                    case "JobException":
                        job.JobException = prop.Value;
                        break;
                    case "JobID":
                        job.JobID = Guid.Parse(prop.Value);
                        break;
                    case "JobStatus":
                        job.JobStatus = prop.Value;
                        break;
                    case "LastModifiedTime":
                        job.LastModifiedTime = !String.IsNullOrEmpty(prop.Value) ? DateTime.Parse(prop.Value) : DateTime.MinValue;
                        break;
                    case "StartTime":
                        job.StartTime = !String.IsNullOrEmpty(prop.Value) ? DateTime.Parse(prop.Value) : DateTime.MinValue;
                        break;
                    case "TenantID":
                        job.TenantID = Guid.Parse(prop.Value);
                        break;
                    case "WarningCount":
                        job.WarningCount = short.Parse(prop.Value);
                        break;
                }
            }

            return job;
        }

        public RunbookViewModel Runbook
        {
            get { return _runbookViewModel; }
        }

        public string WindowTitle
        {
            get
            {
                return _runbookViewModel.RunbookName + ": Execution";
            }
        }

        public ObservableCollection<ExecutionProperty> ExecutionProperties
        {
            get;
            set;
        }

        public string ExecutionContent
        {
            get;
            set;
        }

        public ICommand RunCommand
        {
            get { return _runCommand; }
        }

        public class ExecutionProperty : ObservableObject
        {
            private string _name = string.Empty;
            private object _value = null;

            public ExecutionProperty(string name, object value)
            {
                Name = name;
                Value = value;
            }

            public string Name
            {
                get { return _name; }
                set { _name = value; base.RaisePropertyChanged("Name"); }
            }

            public object Value
            {
                get { return _value; }
                set { _value = value; base.RaisePropertyChanged("Value"); }
            }
        }

        public void Dispose()
        {
            try
            {
                _thread.Abort();
            }
            catch (ThreadAbortException)
            {

            }
        }
    }
}
