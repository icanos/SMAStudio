using System;
using System.Threading.Tasks;
using SMAStudiovNext.Modules.Runbook.ViewModels;
using SMAStudiovNext.Core;
using SMAStudiovNext.Models;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Caliburn.Micro;
using Gemini.Modules.Output;
using System.Net;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using SMAStudiovNext.Vendor.Azure;
using SMAStudiovNext.Icons;
using Gemini.Framework.Services;
using Gemini.Framework;
using System.Windows;
using Newtonsoft.Json;
using System.Security.Cryptography;
using SMAStudiovNext.Language.Completion;

namespace SMAStudiovNext.Services
{
    public class AzureService : IBackendService
    {
        private const string AzureBaseUrl = "https://management.core.windows.net/";
        private const string AzureResourceUrl = "{0}/cloudServices/OaaSCS/resources/automation/~/automationAccounts/{1}/{2}?api-version=2014-12-08";

        private readonly IBackendContext _backendContext;
        private readonly BackendConnection _connectionData;

        private X509Certificate2 _certificate = null;

        public IBackendContext Context
        {
            get { return _backendContext; }
        }

        public AzureService(IBackendContext context, BackendConnection connectionData)
        {
            _backendContext = context;
            _connectionData = connectionData;
        }

        public async Task<bool> CheckIn(RunbookModelProxy runbook)
        {
            return await Task.Run(() =>
            {
                // Publish the draft runbook
                SendRequest("runbooks/" + runbook.RunbookName + "/draft/publish", "POST", "");

                // Move the draft to published
                runbook.PublishedRunbookVersionID = runbook.DraftRunbookVersionID;
                runbook.DraftRunbookVersionID = null;

                return true;
            });
        }

        public async Task<bool> CheckOut(RunbookViewModel runbook)
        {
            return await Task.Run(() =>
            {
                // Check out the runbok
                SendRequest("runbooks/" + runbook.Runbook.RunbookName.ToUrlSafeString() + "/draft/content", "PUT", "");

                // Notify SMA Studio that we have a draft and download the content
                runbook.Runbook.DraftRunbookVersionID = Guid.NewGuid();
                runbook.GetContent(RunbookType.Draft, true);

                return true;
            });
        }

        public Task<bool> CheckRunningJobs(RunbookModelProxy runbook, bool checkDraft)
        {
            return Task.Run(() => { if (runbook.JobID != Guid.Empty) { return true; } else { return false; } });
        }

        public bool Delete(ModelProxyBase model)
        {
            if (model is RunbookModelProxy)
            {
                var runbook = model as RunbookModelProxy;
                SendRequest("runbooks/" + runbook.RunbookName, "DELETE");
            }
            else if (model is VariableModelProxy)
            {
                var variable = model as VariableModelProxy;
                SendRequest("variables/" + variable.Name, "DELETE");
            }
            else if (model is CredentialModelProxy)
            {
                var credential = model as CredentialModelProxy;
                SendRequest("credentials/" + credential.Name, "DELETE");
            }
            else if (model is ScheduleModelProxy)
            {
                var schedule = model as ScheduleModelProxy;
                SendRequest("schedules/" + schedule.Name, "DELETE");
            }

            return true;
        }

        public string SendRequest(string objectType, string requestMethod = "GET", string requestBody = null, string contentType = null)
        {
            if (_certificate == null)
            {
                if (String.IsNullOrEmpty(_connectionData.AzureCertificateThumbprint))
                {
                    var output = IoC.Get<IOutput>();
                    output.AppendLine("Azure Service is enabled but no certificate has been chosen or generated. Please correct this by editing your Azure connection.");

                    return string.Empty;
                }

                _certificate = CertificateManager.FindCertificate(_connectionData.AzureCertificateThumbprint);
            }

            var url = AzureBaseUrl + String.Format(AzureResourceUrl, _connectionData.AzureSubscriptionId, _connectionData.AzureAutomationAccount, objectType);

            return SendRawRequest(url, requestMethod, requestBody, contentType);
        }

        private string SendRawRequest(string url, string requestMethod = "GET", string requestBody = null, string contentType = null)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = requestMethod;
            request.Accept = "application/json";
            request.Headers.Add("x-ms-version", "2013-06-01");
            request.ClientCertificates.Add(_certificate);

            if (contentType != null)
                request.ContentType = contentType;

            if (requestBody != null)
            {
                request.ContentLength = requestBody.Length;

                var requestStream = new StreamWriter(request.GetRequestStream());
                requestStream.Write(requestBody);
                requestStream.Flush();
                requestStream.Close();
            }

            var response = (HttpWebResponse)request.GetResponse();

            if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.Accepted)
            {
                var responseStream = new StreamReader(response.GetResponseStream());

                string content = responseStream.ReadToEnd();

                responseStream.Close();
                response.Close();

                return content;
            }

            response.Close();

            MessageBox.Show("The Azure Automation API is currently unavailable. Please try again later.", "Connectivity Issues", MessageBoxButton.OK);

            return string.Empty;
        }

        public JobModelProxy GetJobDetails(RunbookModelProxy runbook)
        {
            if (runbook.IsTestRun)
                return GetDraftJobDetails(runbook);

            if (runbook.JobID != Guid.Empty)
                return GetPublishedJobDetails(runbook.JobID);

            return new JobModelProxy(new Job(), Context);
        }

        public JobModelProxy GetJobDetails(Guid jobId)
        {
            return GetPublishedJobDetails(jobId);
        }

        private JobModelProxy GetDraftJobDetails(RunbookModelProxy runbook)
        {
            var result = SendRequest("runbooks/" + runbook.RunbookName.ToUrlSafeString() + "/draft/testJob");

            if (result.Length < 1)
                return null;

            dynamic jsonJob = JObject.Parse(result);

            var job = new Job();
            job.JobID = Guid.NewGuid();
            job.CreationTime = (DateTime)jsonJob.creationTime;
            job.LastModifiedTime = (DateTime)jsonJob.lastModifiedTime;
            job.JobStatus = jsonJob.status;
            job.StartTime = jsonJob.startTime != null ? (DateTime?)jsonJob.startTime : null;
            job.EndTime = jsonJob.endTime != null ? (DateTime?)jsonJob.endTime : null;

            var jobModelProxy = new JobModelProxy(job, Context);

            foreach (var param in jsonJob.parameters)
            {
                //jobModelProxy.Parameters.Add(param)
            }

            var output = SendRequest("runbooks/" + runbook.RunbookName.ToUrlSafeString() + "/draft/testJob/streams");
            jobModelProxy = ParseJobStreams(jobModelProxy, output);

            return jobModelProxy;
        }

        private JobModelProxy GetPublishedJobDetails(Guid jobId)
        {
            var result = SendRequest("jobs/" + jobId);

            if (result.Length == 0) // maybe return null instead?
                return null;

            dynamic jsonJob = JObject.Parse(result);

            var job = new Job();
            job.JobID = jobId;
            job.CreationTime = (DateTime)jsonJob.properties.creationTime;
            job.LastModifiedTime = (DateTime)jsonJob.properties.lastModifiedTime;
            job.JobStatus = jsonJob.properties.status;
            job.StartTime = jsonJob.properties.startTime != null ? (DateTime?)jsonJob.properties.startTime : null;
            job.EndTime = jsonJob.properties.endTime != null ? (DateTime?)jsonJob.properties.endTime : null;

            var jobModelProxy = new JobModelProxy(job, Context);

            foreach (var param in jsonJob.properties.parameters)
            {
                //jobModelProxy.Parameters.Add(param)
            }

            var output = SendRequest("jobs/" + jobId + "/streams");
            jobModelProxy = ParseJobStreams(jobModelProxy, output);

            if (jsonJob.properties.exception != null)
            {
                jobModelProxy.Result.Add(new JobOutput()
                {
                    JobID = jobId,
                    RunbookVersionID = Guid.Empty,
                    StreamTypeName = "Error",
                    StreamTime = DateTime.Now,
                    StreamText = jsonJob.properties.exception,
                    TenantID = Guid.Empty
                });
            }

            return jobModelProxy;
        }

        private JobModelProxy ParseJobStreams(JobModelProxy jobModelProxy, string output)
        {
            dynamic jsonOutput = JObject.Parse(output);

            if (jsonOutput != null && jsonOutput.value != null)
            {
                foreach (var outputRow in jsonOutput.value)
                {
                    if (outputRow.properties.time > jobModelProxy.LastDownloadTime)
                    {
                        jobModelProxy.Result.Add(new JobOutput()
                        {
                            JobID = jobModelProxy.JobID,
                            RunbookVersionID = Guid.Empty,
                            StreamTypeName = outputRow.properties.streamType,
                            StreamTime = outputRow.properties.time,
                            StreamText = outputRow.properties.summary,
                            TenantID = Guid.Empty
                        });
                    }
                }
            }

            return jobModelProxy;
        }

        public IList<JobModelProxy> GetJobs(Guid runbookVersionId)
        {
            // Since Azure are so nice, not providing any IDs for our runbooks and instead
            // using the name to identify the runbooks (as contrary to SMA which uses GUIDs),
            // we need to query our backend context to retrieve the correct runbook based on
            // our genereated GUID
            var runbook = Context.Runbooks.FirstOrDefault(r => (r.Tag as RunbookModelProxy).PublishedRunbookVersionID == runbookVersionId);

            if (runbook == null) // no runbook found?
                return new List<JobModelProxy>();

            var jobs = new List<JobModelProxy>();
            var result = SendRequest("jobs");
            dynamic jsonJobs = JObject.Parse(result);

            if (jsonJobs.value != null)
            {
                foreach (var job in jsonJobs.value)
                {
                    if (job.properties.runbook.name != (runbook.Tag as RunbookModelProxy).RunbookName)
                        continue;

                    var azureJob = new Job();
                    azureJob.JobID = job.properties.jobId;
                    azureJob.JobStatus = job.properties.status;
                    azureJob.CreationTime = job.properties.creationTime;
                    azureJob.StartTime = job.properties.startTime;
                    azureJob.LastModifiedTime = job.properties.lastModifiedTime;
                    azureJob.EndTime = job.properties.endTime;

                    jobs.Add(new JobModelProxy(azureJob, Context));
                }
            }

            return jobs;
        }

        public void Load()
        {
            if (SettingsService.CurrentSettings == null)
                return;

            AsyncExecution.Run(System.Threading.ThreadPriority.Normal, () =>
            {
                // Load all runbooks
                var runbooksContent = SendRequest("runbooks");

                if (runbooksContent.Length != 0)
                {
                    dynamic runbooksRaw = JObject.Parse(runbooksContent);

                    foreach (var entry in runbooksRaw.value)
                    {
                        var runbook = new Runbook();
                        runbook.RunbookID = Guid.NewGuid();
                        runbook.RunbookName = entry.name;
                        runbook.State = entry.properties.state;
                        runbook.Tags = entry.properties.serviceManagementTags;

                        _backendContext.Runbooks.Add(new ResourceContainer(runbook.RunbookName, new RunbookModelProxy(runbook, this.Context), IconsDescription.Runbook));
                    }

                    _backendContext.ParseTags();
                    _backendContext.IsReady = true;

                    runbooksRaw = null;
                }

                Execute.OnUIThread(() =>
                {
                    _backendContext.SignalCompleted();
                });
            });

            AsyncExecution.Run(System.Threading.ThreadPriority.Normal, () =>
            {
                // Load all variables
                var variablesContent = SendRequest("variables");

                if (variablesContent.Length > 0)
                {
                    dynamic variablesRaw = JObject.Parse(variablesContent);

                    foreach (var entry in variablesRaw.value)
                    {
                        var variable = new Variable();
                        variable.VariableID = Guid.NewGuid();
                        variable.Name = entry.name;
                        variable.IsEncrypted = entry.properties.isEncrypted;
                        variable.Value = (entry.properties.value != null ? entry.properties.value : "");

                        //_backendContext.Variables.Add(new ResourceContainer(variable.Name, new VariableModelProxy(variable, Context), IconsDescription.Variable));
                        _backendContext.AddToVariables(new VariableModelProxy(variable, Context));
                    }

                    variablesRaw = null;
                }
            });

            AsyncExecution.Run(System.Threading.ThreadPriority.Normal, () =>
            {
                // Load all credentials
                var credentialsContent = SendRequest("credentials");

                if (credentialsContent.Length > 0)
                {
                    dynamic credentialsRaw = JObject.Parse(credentialsContent);

                    foreach (var entry in credentialsRaw.value)
                    {
                        var credential = new Credential();
                        credential.CredentialID = Guid.NewGuid();
                        credential.Name = entry.name;
                        credential.UserName = entry.properties.userName;

                        _backendContext.AddToCredentials(new CredentialModelProxy(credential, Context));
                    }

                    credentialsRaw = null;
                }
            });

            AsyncExecution.Run(System.Threading.ThreadPriority.Normal, () =>
            {
                // Load all schedules
                var schedulesContent = SendRequest("schedules");

                if (schedulesContent.Length > 0)
                {
                    dynamic schedulesRaw = JObject.Parse(schedulesContent);

                    foreach (var entry in schedulesRaw.value)
                    {
                        var schedule = new Schedule();
                        schedule.ScheduleID = Guid.NewGuid();
                        schedule.Name = entry.name;
                        schedule.StartTime = entry.properties.startTime;
                        schedule.ExpiryTime = entry.properties.expiryTime;
                        schedule.IsEnabled = entry.properties.isEnabled;

                        if (entry.properties.frequency.Equals("day"))
                            schedule.DayInterval = entry.properties.interval;
                        else if (entry.properties.frequency.Equals("hour"))
                            schedule.HourInterval = entry.properties.interval;

                        _backendContext.AddToSchedules(new ScheduleModelProxy(schedule, Context));
                    }

                    schedulesRaw = null;
                }
            });
        }

        public void PauseExecution(Guid jobId)
        {
            SendRequest("jobs/" + jobId + "/suspend", "POST", "", "0");
        }

        public void ResumeExecution(Guid jobId)
        {
            SendRequest("jobs/" + jobId + "/resume", "POST", "", "0");
        }

        public void Save(IViewModel instance)
        {
            if (instance.Model is RunbookModelProxy)
            {
                SaveAzureRunbook(instance);
            }
            else if (instance.Model is VariableModelProxy)
            {
                SaveAzureVariable(instance);
            }
            else if (instance.Model is CredentialModelProxy)
            {
                SaveAzureCredential(instance);
            }
            else if (instance.Model is ScheduleModelProxy)
            {
                SaveAzureSchedule(instance);
            }
            else
                throw new NotImplementedException();

            // And lastly, open the document (or put focus on it if its open)
            var shell = IoC.Get<IShell>();
            shell.OpenDocument((IDocument)instance);
        }

        private void SaveAzureRunbook(IViewModel viewModel)
        {
            var runbook = viewModel.Model as RunbookModelProxy;

            if (runbook.RunbookID == Guid.Empty)
            {
                // New runbook that doesn't exist in Azure Automation yet
                var json = new Dictionary<string, object>();
                var properties = new Dictionary<string, object>();
                properties.Add("runbookType", "Script");
                properties.Add("logProgress", false);
                properties.Add("logVerbose", false);

                var draft = new Dictionary<string, object>();
                draft.Add("inEdit", true);
                draft.Add("creationTime", DateTime.Now);
                draft.Add("lastModifiedTime", DateTime.Now);
                properties.Add("draft", draft);

                json.Add("properties", properties);

                var cryptoProvider = new SHA256CryptoServiceProvider();
                var encoding = System.Text.Encoding.UTF8;

                var rbBytes = encoding.GetBytes(viewModel.Content);
                var resultHash = cryptoProvider.ComputeHash(rbBytes);
                var resultHashB64 = Convert.ToBase64String(resultHash);

                SendRequest("runbooks/" + runbook.RunbookName.ToUrlSafeString(), "PUT", JsonConvert.SerializeObject(json), "application/json");
            }
            
            // Update the runbook
            SendRequest("runbooks/" + runbook.RunbookName.ToUrlSafeString() + "/draft/content", "PUT", viewModel.Content, "text/powershell");

            // Reset the unsaved changes flag
            viewModel.UnsavedChanges = false;
        }

        private void SaveAzureVariable(IViewModel viewModel)
        {
            var variable = viewModel.Model as VariableModelProxy;

            var dict = new Dictionary<string, object>();
            var properties = new Dictionary<string, object>();
            properties.Add("isEncrypted", variable.IsEncrypted);
            properties.Add("value", variable.Value);
            dict.Add("properties", properties);

            if (variable.VariableID == Guid.Empty)
            {
                // New variable
                SendRequest("variables/" + variable.Name.ToUrlSafeString(), "PUT", JsonConvert.SerializeObject(dict), "application/json");
            }
            else
            {
                SendRequest("variables/" + variable.Name.ToUrlSafeString(), "PATCH", JsonConvert.SerializeObject(dict), "application/json");
            }

            viewModel.UnsavedChanges = false;
        }

        private void SaveAzureCredential(IViewModel viewModel)
        {
            var credential = viewModel.Model as CredentialModelProxy;

            var dict = new Dictionary<string, object>();
            var properties = new Dictionary<string, object>();
            properties.Add("userName", credential.UserName);
            properties.Add("password", credential.RawValue);
            dict.Add("properties", properties);

            if (credential.CredentialID == Guid.Empty)
            {
                // New variable
                SendRequest("credentials/" + credential.Name.ToUrlSafeString(), "PUT", JsonConvert.SerializeObject(dict), "application/json");
            }
            else
            {
                SendRequest("credentials/" + credential.Name.ToUrlSafeString(), "PATCH", JsonConvert.SerializeObject(dict), "application/json");
            }

            viewModel.UnsavedChanges = false;
        }

        private void SaveAzureSchedule(IViewModel viewModel)
        {
            var schedule = viewModel.Model as ScheduleModelProxy;

            var dict = new Dictionary<string, object>();
            var properties = new Dictionary<string, object>();

            if (schedule.ScheduleID != Guid.Empty)
            {
                // Only supported when creating a new schedule
                properties.Add("startTime", schedule.StartTime.ToUniversalTime());

                if (schedule.ExpiryTime.HasValue)
                    properties.Add("startTime", schedule.ExpiryTime.Value.ToUniversalTime());
            }

            properties.Add("isEnabled", schedule.IsEnabled);

            if (schedule.ScheduleID != Guid.Empty)
            {
                // Only supported when creating a new schedule
                if ((schedule.Model as Schedule).DayInterval > 0)
                {
                    properties.Add("frequency", "day");
                    properties.Add("interval", (schedule.Model as Schedule).DayInterval);
                }
                else if ((schedule.Model as Schedule).HourInterval > 0)
                {
                    properties.Add("frequency", "hour");
                    properties.Add("interval", (schedule.Model as Schedule).HourInterval);
                }
                else
                    properties.Add("frequency", "onetime");
            }

            dict.Add("properties", properties);

            if (schedule.ScheduleID == Guid.Empty)
            {
                // New variable
                SendRequest("schedules/" + schedule.Name.ToUrlSafeString(), "PUT", JsonConvert.SerializeObject(dict), "application/json");
            }
            else
            {
                SendRequest("schedules/" + schedule.Name.ToUrlSafeString(), "PATCH", JsonConvert.SerializeObject(dict), "application/json");
            }

            viewModel.UnsavedChanges = false;
        }

        public Guid? StartRunbook(RunbookModelProxy runbookProxy, List<SMA.NameValuePair> parameters)
        {
            var jobGuid = Guid.NewGuid();

            var runbook = new Dictionary<string, string>();
            runbook.Add("name", runbookProxy.RunbookName);

            var jobParameters = new Dictionary<string, string>();
            foreach (var parameter in parameters)
            {
                jobParameters.Add(parameter.Name, parameter.Value);
            }

            var job = new Dictionary<string, Dictionary<string, object>>();
            job.Add("properties", new Dictionary<string, object> { { "runbook", runbook } });
            job["properties"].Add("parameters", jobParameters);

            var json = JsonConvert.SerializeObject(job);

            runbookProxy.IsTestRun = false;

            try
            {
                SendRequest("jobs/" + jobGuid, "PUT", json, "application/json");
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError && (ex.Response as HttpWebResponse).StatusCode == HttpStatusCode.BadRequest)
                    MessageBox.Show("A job is already running, please wait for that to complete and then try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                else
                    MessageBox.Show("An unknown error occurred when trying to start the runbook: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                return null;
            }

            return jobGuid;
        }

        public void StopExecution(Guid jobId)
        {
            SendRequest("jobs/" + jobId + "/stop", "POST", "", "0");
        }

        public Guid? TestRunbook(RunbookModelProxy runbookProxy, List<SMA.NameValuePair> parameters)
        {
            var jobGuid = Guid.NewGuid(); // Not used in this case, which is a bit confusing :(

            var runbook = new Dictionary<string, string>();
            runbook.Add("name", runbookProxy.RunbookName);

            var jobParameters = new Dictionary<string, string>();
            foreach (var parameter in parameters)
            {
                jobParameters.Add(parameter.Name, parameter.Value);
            }

            var job = new Dictionary<string, Dictionary<string, object>>();
            job.Add("properties", new Dictionary<string, object> { { "parameters", jobParameters } });
            var json = JsonConvert.SerializeObject(job);

            runbookProxy.IsTestRun = true;

            // Try to start the runbook job
            try
            {
                SendRequest("runbooks/" + runbookProxy.RunbookName.ToUrlSafeString() + "/draft/testJob", "PUT", json, "application/json");
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError && (ex.Response as HttpWebResponse).StatusCode == HttpStatusCode.BadRequest)
                    MessageBox.Show("A job is already running, please wait for that to complete and then try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                else
                    MessageBox.Show("An unknown error occurred when trying to test the runbook: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                return null;
            }

            return jobGuid;
        }

        public string GetContent(string url)
        {
            return SendRawRequest(url);
        }

        public string GetBackendUrl(RunbookType runbookType, RunbookModelProxy runbook)
        {
            switch (runbookType)
            {
                case RunbookType.Draft:
                    return AzureBaseUrl + String.Format(AzureResourceUrl, _connectionData.AzureSubscriptionId, _connectionData.AzureAutomationAccount, "runbooks/" + runbook.RunbookName + "/draft/content");
                case RunbookType.Published:
                    return AzureBaseUrl + String.Format(AzureResourceUrl, _connectionData.AzureSubscriptionId, _connectionData.AzureAutomationAccount, "runbooks/" + runbook.RunbookName + "/content");
            }

            return string.Empty;
        }

        public IList<ICompletionEntry> GetParameters(RunbookViewModel runbookViewModel, KeywordCompletionData completionData)
        {
            throw new NotImplementedException();
        }
    }
}
