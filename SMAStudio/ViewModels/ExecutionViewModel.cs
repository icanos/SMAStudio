using SMAStudio.Commands;
using SMAStudio.Models;
using SMAStudio.Services;
using SMAStudio.Settings;
using SMAStudio.SMAWebService;
using SMAStudio.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
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
    /// <summary>
    /// TODO: Move the job retriever to a separate service
    /// </summary>
    public class ExecutionViewModel : ObservableObject, IDisposable
    {
        private IApiService _api;
        private RunbookViewModel _runbookViewModel;

        private ExecutionProperty paramStartTime = null;
        private ExecutionProperty paramEndTime = null;
        private ExecutionProperty paramErrorCount = null;
        private ExecutionProperty paramWarnCount = null;
        private ExecutionProperty paramJobStatus = null;

        private Thread _thread = null;
        private Job _job = null;

        public ExecutionViewModel(RunbookViewModel runbookViewModel)
        {
            _runbookViewModel = runbookViewModel;

            _api = Core.Resolve<IApiService>();
            ExecutionProperties = new ObservableCollection<ExecutionProperty>();

            // Start the retrieving of data from the webservice
            Run();
        }

        /// <summary>
        /// Entry point to where the execution logging takes place
        /// </summary>
        public void Run()
        {
            var doneStatuses = new List<string> { "Completed", "Failed" };

            //AsyncService.Execute(ThreadPriority.Normal, delegate()
            _thread = new Thread(new ThreadStart(delegate()
            {
                _job = GetJobDetails(_runbookViewModel.JobID);

                //var job = _api.Current.Jobs.Where(j => j.JobID == _runbookViewModel.JobID).FirstOrDefault();

                if (_job == null)
                    return;

                // Add the JobID parameter
                var param1 = new ExecutionProperty("Job ID", _job.JobID);
                var param2 = new ExecutionProperty("Runbook ID", _runbookViewModel.Runbook.RunbookID);
                var param3 = new ExecutionProperty("Runbook Name", _runbookViewModel.Runbook.RunbookName);
                paramJobStatus = new ExecutionProperty("Job Status", _job.JobStatus);
                paramStartTime = new ExecutionProperty("Start Time", _job.StartTime);
                paramEndTime = new ExecutionProperty("End Time", _job.EndTime);
                var param5 = new ExecutionProperty("Creation Time", _job.CreationTime);
                var param6 = new ExecutionProperty("Last Modified Time", _job.LastModifiedTime);
                paramErrorCount = new ExecutionProperty("Error Count", _job.ErrorCount);
                paramWarnCount = new ExecutionProperty("Warning Count", _job.WarningCount);

                App.Current.Dispatcher.Invoke((Action)delegate()
                {
                    ExecutionProperties.Add(param1);
                    ExecutionProperties.Add(param2);
                    ExecutionProperties.Add(param3);
                    ExecutionProperties.Add(paramJobStatus);
                    ExecutionProperties.Add(paramStartTime);
                    ExecutionProperties.Add(paramEndTime);
                    ExecutionProperties.Add(param5);
                    ExecutionProperties.Add(param6);
                    ExecutionProperties.Add(paramErrorCount);
                    ExecutionProperties.Add(paramWarnCount);
                });

                while (!doneStatuses.Contains(_job.JobStatus))
                {
                    UpdateExecution(_job);

                    Thread.Sleep(2 * 1000);
                    var tmpJob = GetJobDetails(_runbookViewModel.JobID);

                    // If the job we retrieved is null - we break, since the job was most
                    // likely cancelled by the user or the runbook server going offline.
                    if (tmpJob == null)
                    {
                        _job.JobStatus = "Stopped";
                        _job.EndTime = DateTime.Now;
                        break;
                    }
                    else
                        _job = tmpJob;
                }

                UpdateExecution(_job);

                Console.WriteLine("Execution is complete.");
                base.RaisePropertyChanged("ExecutionProperties");
                _runbookViewModel.JobID = Guid.Empty;
            }));

            _thread.Start();
        }

        /// <summary>
        /// Verifies if a job is running and in case it is, warns the user before closing
        /// </summary>
        public void ClosingWindow()
        {
            if (_job.JobStatus.Equals("New") ||
                _job.JobStatus.Equals("Running") ||
                _job.JobStatus.Equals("Activating") ||
                _job.JobStatus.Equals("Suspended"))
            {
                if (MessageBox.Show("The job is currently running. Do you want to cancel the job before closing the window?", "Cancel job", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    _job.Stop(_api.Current);
                }
            }
        }

        /// <summary>
        /// Loop running while the job is executing, downloading new information from the web service.
        /// </summary>
        /// <param name="job">The job we're executing</param>
        private void UpdateExecution(Job job)
        {
            if (App.Current == null)
            {
                _thread.Abort();
            }

            App.Current.Dispatcher.Invoke((Action)delegate()
            {
                ExecutionContent = job.JobException;

                if (!String.IsNullOrEmpty(job.JobException))
                    ExecutionContent += "\r\n\r\n--------------------------------------------------\r\n\r\n";

                //ExecutionContent += ApiHelpers.GetJobOutput(_api.Current, apiJob);
                ExecutionContent += GetJobOutput(job);

                paramStartTime.Value = job.StartTime;
                paramEndTime.Value = job.EndTime;
                paramErrorCount.Value = job.ErrorCount;
                paramWarnCount.Value = job.WarningCount;
                paramJobStatus.Value = job.JobStatus;

                base.RaisePropertyChanged("ExecutionContent");
                base.RaisePropertyChanged("ExecutionProperties");
            });
        }
        
        /// <summary>
        /// Retrieves the job details from SMA web service
        /// 
        /// I had to write this method instead of using the Service Reference since data from
        /// this command was cached and not updated, resulting in a endless loop of waiting for
        /// the job to complete, even though it did a long time ago.
        /// </summary>
        /// <param name="jobGuid">GUID to retrieve information about</param>
        /// <returns></returns>
        private Job GetJobDetails(Guid jobGuid)
        {
            string url = SettingsManager.Current.Settings.SmaWebServiceUrl + "/Jobs(guid'" + jobGuid + "')";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Credentials = CredentialCache.DefaultCredentials;
            HttpWebResponse response = null;

            try
            {
                response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException)
            {
                return null;
            }

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
        
        /// <summary>
        /// Retrieves output from a job execution by reading the output stream
        /// 
        /// I had to write this method in order to be able to retrieve any stream
        /// we wanted, instead of just the Output stream. This was discovered by
        /// running Wireshark while using the PowerShell cmdlets of SMA.
        /// </summary>
        /// <param name="job">Which job to retrieve output from</param>
        /// <param name="streamType">Which stream to retrieve</param>
        /// <returns>Formatted output from the job</returns>
        private string GetJobOutput(Job job, string streamType = "Any")
        {
            string jobStreamUrl = SettingsManager.Current.Settings.SmaWebServiceUrl + "/JobStreams/GetStreamItems";
            string queryString = "jobId='" + job.JobID.ToString() + "'&streamType='" + streamType + "'";

            if (job.StartTime != null)
            {
                queryString += "&streamsCreatedSinceDateTime='" + Uri.EscapeDataString(((DateTime)job.StartTime).ToUniversalTime().ToString()) + "'";
            }

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(jobStreamUrl + "?" + queryString);
            request.Credentials = CredentialCache.DefaultCredentials;
            HttpWebResponse response = null;

            try
            {
                response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException e)
            {
                Core.Log.Error("Error when trying to load job data.", e);
                return string.Empty;
            }

            TextReader tr = new StreamReader(response.GetResponseStream());
            string content = tr.ReadToEnd();

            tr.Close();

            XElement outputXml = XElement.Parse(content);
            XNamespace d = "http://schemas.microsoft.com/ado/2007/08/dataservices";
            XNamespace m = "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata";
            XNamespace a = "http://www.w3.org/2005/Atom";
            IEnumerable<XElement> entries = outputXml.Elements(a + "entry");//.Elements(m + "properties");

            List<OutputItem> outputItems = new List<OutputItem>();
            foreach (var entry in entries)
            {
                var propertyContainers = entry.Element(a + "content").Element(m + "properties").Elements();
                var outputItem = new OutputItem();

                foreach (var prop in propertyContainers)
                {
                    switch (prop.Name.LocalName)
                    {
                        case "JobId":
                            outputItem.JobID = Guid.Parse(prop.Value);
                            break;
                        case "RunbookVersionId":
                            outputItem.RunbookVersionID = Guid.Parse(prop.Value);
                            break;
                        case "StreamTypeName":
                            outputItem.StreamTypeName = prop.Value;
                            break;
                        case "TenantId":
                            outputItem.TenantID = Guid.Parse(prop.Value);
                            break;
                        case "StreamTime":
                            outputItem.StreamTime = DateTime.Parse(prop.Value);
                            break;
                        case "StreamText":
                            outputItem.StreamText = prop.Value;
                            break;
                        case "NameValues":
                            /*var subProps = prop.Element(d + "element").Element(d + "NameValueInner").Elements(d + "element");

                            foreach (var subProp in subProps)
                            {
                                outputItem.Values.Add(new OutputNameValue
                                    {
                                        Name = subProp.Element(d + "Name").Value,
                                        Value = subProp.Element(d + "Value").Value
                                    });
                            }*/
                            break;
                    }
                }

                outputItems.Add(outputItem);
            }

            string resultContent = "";

            foreach (var item in outputItems)
                resultContent += item.ToString() + "\r\n\r\n";

            return resultContent;
        }

        #region Properties
        /// <summary>
        /// Gets the RunbookViewModel this execution context is focused around
        /// </summary>
        public RunbookViewModel Runbook
        {
            get { return _runbookViewModel; }
        }

        /// <summary>
        /// Gets the window title (used for data binding)
        /// </summary>
        public string WindowTitle
        {
            get
            {
                return "Execution: " + _runbookViewModel.RunbookName;
            }
        }

        /// <summary>
        /// Gets or sets the execution properties, the information displayed to the left
        /// in the window.
        /// </summary>
        public ObservableCollection<ExecutionProperty> ExecutionProperties
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the output from the runbook execution
        /// </summary>
        public string ExecutionContent
        {
            get;
            set;
        }

        public ICommand RunCommand
        {
            get { return Core.Resolve<ICommand>("Run"); }
        }

        public ICommand StopCommand
        {
            get { return Core.Resolve<ICommand>("Stop"); }
        }
        #endregion

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
