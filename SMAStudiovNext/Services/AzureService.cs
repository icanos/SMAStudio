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
using SMAStudiovNext.Modules.Runbook.CodeCompletion;
using Newtonsoft.Json;

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

        public Task<bool> CheckOut(RunbookViewModel runbook)
        {
            throw new NotImplementedException();
        }

        public Task<bool> CheckRunningJobs(RunbookModelProxy runbook, bool checkDraft)
        {
            // Don't know how to implement this yet
            return Task.Run(() => { return false; });
        }

        public bool Delete(ModelProxyBase model)
        {
            if (model is RunbookModelProxy)
            {
                var runbook = model as RunbookModelProxy;
                SendRequest("runbooks/" + runbook.RunbookName, "DELETE");
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

            if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created)
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

        public JobModelProxy GetJobDetails(Guid jobId)
        {
            var result = SendRequest("jobs/" + jobId);

            if (result.Length == 0) // maybe return null instead?
                return new JobModelProxy(new Job(), Context);

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
            dynamic jsonOutput = JObject.Parse(output);

            if (jsonOutput != null && jsonOutput.value != null)
            {
                foreach (var outputRow in jsonOutput.value)
                {
                    jobModelProxy.Result.Add(new JobOutput()
                    {
                        JobID = jobId,
                        RunbookVersionID = Guid.Empty,
                        StreamTypeName = outputRow.properties.streamType,
                        StreamTime = outputRow.properties.time,
                        StreamText = outputRow.properties.summary,
                        TenantID = Guid.Empty
                    });
                }
            }
            
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

                        _backendContext.Variables.Add(new ResourceContainer(variable.Name, new VariableModelProxy(variable, Context), IconsDescription.Variable));
                    }
                }

                Execute.OnUIThread(() =>
                {
                    _backendContext.SignalCompleted();
                });
            });
        }

        public void PauseExecution(Guid jobId)
        {
            throw new NotImplementedException();
        }

        public void ResumeExecution(Guid jobId)
        {
            throw new NotImplementedException();
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
                /*var proxy = (CredentialModelProxy)instance.Model;

                if (proxy.GetSubType().Equals(typeof(SMA.Credential)))
                    SaveSmaCredential(context, instance);*/
            }
            else if (instance.Model is ScheduleModelProxy)
            {
                /*var proxy = (ScheduleModelProxy)instance.Model;

                if (proxy.GetSubType().Equals(typeof(SMA.Schedule)))
                    SaveSmaSchedule(context, instance);*/
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
                MessageBox.Show("Not supported yet as we need to utilize Azure Storage as well.");
            }
            else
            {
                // Update the runbook
                SendRequest("runbooks/" + runbook.RunbookName + "/draft/content", "PUT", viewModel.Content, "text/powershell");

                // Reset the unsaved changes flag
                viewModel.UnsavedChanges = false;
            }
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
                SendRequest("variables/" + variable.Name.Replace(" ", "%20"), "PUT", JsonConvert.SerializeObject(dict), "application/json");
            }
            else
            {
                SendRequest("variables/" + variable.Name.Replace(" ", "%20"), "PATCH", JsonConvert.SerializeObject(dict), "application/json");
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

            SendRequest("jobs/" + jobGuid, "PUT", json, "application/json");

            return jobGuid;
        }

        public void StopExecution(Guid jobId)
        {
            throw new NotImplementedException();
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
            //job["properties"].Add("parameters", jobParameters);

            var json = JsonConvert.SerializeObject(job);

            SendRequest("runbooks/" + runbookProxy.RunbookName + "/draft/testJob", "PUT", json, "application/json");

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
