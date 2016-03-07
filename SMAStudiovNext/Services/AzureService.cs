using Caliburn.Micro;
using Gemini.Framework;
using Gemini.Framework.Commands;
using Gemini.Framework.Services;
using Gemini.Modules.Output;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SMAStudiovNext.Core;
using SMAStudiovNext.Core.Exceptions;
using SMAStudiovNext.Core.Tracing;
using SMAStudiovNext.Icons;
using SMAStudiovNext.Models;
using SMAStudiovNext.Modules.Runbook.Editor.Completion;
using SMAStudiovNext.Modules.Runbook.ViewModels;
using SMAStudiovNext.Utils;
using SMAStudiovNext.Vendor.Azure;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;

namespace SMAStudiovNext.Services
{
    public enum OperationStatus
    {
        InProgress,
        Succeeded,
        Failed
    }

    public class AzureService : IBackendService
    {
        private const string AzureBaseUrl = "https://management.core.windows.net/";
        private const string AzureResourceUrl = "{0}/cloudServices/OaaSCS/resources/automation/~/automationAccounts/{1}/{2}?api-version=2014-12-08";
        private const string AzureXNS = "http://schemas.microsoft.com/windowsazure";

        private readonly IBackendContext _backendContext;
        private readonly BackendConnection _connectionData;
        private readonly HttpClient _httpClient;
        private readonly WebRequestHandler _webRequestHandler;

        private readonly List<string> _completionStatusList = new List<string> { "Completed", "Failed", "Stopped" };

        private X509Certificate2 _certificate = null;

        public IBackendContext Context
        {
            get { return _backendContext; }
        }

        public AzureService(IBackendContext context, BackendConnection connectionData)
        {
            _backendContext = context;
            _connectionData = connectionData;

            if (_certificate == null)
            {
                if (String.IsNullOrEmpty(_connectionData.AzureCertificateThumbprint))
                {
                    throw new ApplicationException("Azure Service is enabled but no certificate has been chosen or generated. Please correct this by editing your Azure connection.");
                }

                _certificate = CertificateManager.FindCertificate(_connectionData.AzureCertificateThumbprint);
                _webRequestHandler = new WebRequestHandler();
                _webRequestHandler.ClientCertificates.Add(_certificate);
                _httpClient = new HttpClient(_webRequestHandler);
            }
        }

        #region General (API, global DELETE/SAVE etc)

        public bool Delete(ModelProxyBase model)
        {
            var invocationId = string.Empty;

            if (TracingAdapter.IsEnabled)
            {
                invocationId = TracingAdapter.NextInvocationId.ToString();
                TracingAdapter.Enter(invocationId, this, "Delete", new Dictionary<string, object>()
                {
                    {
                        "model",
                        model
                    }
                });
            }

            if (model is RunbookModelProxy)
            {
                var runbook = model as RunbookModelProxy;
                SendRequest("runbooks/" + runbook.RunbookName, HttpMethod.Delete);
            }
            else if (model is VariableModelProxy)
            {
                var variable = model as VariableModelProxy;
                SendRequest("variables/" + variable.Name.ToUrlSafeString(), HttpMethod.Delete);
            }
            else if (model is CredentialModelProxy)
            {
                var credential = model as CredentialModelProxy;
                SendRequest("credentials/" + credential.Name.ToUrlSafeString(), HttpMethod.Delete);
            }
            else if (model is ScheduleModelProxy)
            {
                var schedule = model as ScheduleModelProxy;
                SendRequest("schedules/" + schedule.Name.ToUrlSafeString(), HttpMethod.Delete);
            }
            else if (model is ModuleModelProxy)
            {
                var module = model as ModuleModelProxy;
                SendRequest("modules/" + module.ModuleName.ToUrlSafeString(), HttpMethod.Delete);
            }
            else if (model is ConnectionModelProxy)
            {
                var connection = model as ConnectionModelProxy;
                SendRequest("connections/" + connection.Name.ToUrlSafeString(), HttpMethod.Delete);
            }

            if (TracingAdapter.IsEnabled)
            {
                TracingAdapter.Exit(invocationId, null);
            }

            return true;
        }

        public async Task<OperationResult> Save(IViewModel instance, Command command)
        {
            var operationResult = default(OperationResult);

            if (instance.Model is RunbookModelProxy)
            {
                operationResult = await SaveAzureRunbook(instance);
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
            else if (instance.Model is ModuleModelProxy)
            {
                SaveAzureModule(instance);
            }
            else if (instance.Model is ConnectionModelProxy)
            {
                SaveAzureConnection(instance);
            }
            else
                throw new NotImplementedException();

            // And lastly, open the document (or put focus on it if its open)
            var shell = IoC.Get<IShell>();
            shell.OpenDocument((IDocument)instance);
            
            if (command != null)
                Execute.OnUIThread(() => { command.Enabled = true; });

            return operationResult;
        }

        public string SendRequest(string objectType, HttpMethod requestMethod, string requestBody = null, string contentType = null)
        {
            return AsyncHelper.RunSync<string>(() => SendRequestAsync(objectType, requestMethod, requestBody, contentType));
        }

        public async Task<string> SendRequestAsync(string objectType, HttpMethod requestMethod, string requestBody = null, string contentType = null)
        {
            var url = AzureBaseUrl + String.Format(AzureResourceUrl, _connectionData.AzureSubscriptionId, _connectionData.AzureAutomationAccount, objectType);

            return await SendRawRequestAsync(url, requestMethod, requestBody, contentType);
        }

        public string SendRawRequest(string url, HttpMethod requestMethod, string requestBody = null, string contentType = null)
        {
            return AsyncHelper.RunSync<string>(() => SendRawRequestAsync(url, requestMethod, requestBody, contentType));
        }

        /// <summary>
        /// Executes a raw HTTP request and returns a x-ms-request-id if retrieved. This ID is used in async operations to determine status
        /// of the job that is executed.
        /// </summary>
        /// <param name="url">URL to send the request to</param>
        /// <param name="requestMethod">HTTP method to use (GET, POST, PUT, DELETE etc)</param>
        /// <param name="requestBody">Body to send with the request</param>
        /// <param name="contentType">Content type of the request</param>
        /// <returns>x-ms-request-id if existing in the response</returns>
        private async Task<string> SendRawRequestAsync(string url, HttpMethod requestMethod, string requestBody = null, string contentType = null, bool retryAutomatically = true)
        {
            var invocationId = string.Empty;

            if (TracingAdapter.IsEnabled)
            {
                invocationId = TracingAdapter.NextInvocationId.ToString();
                TracingAdapter.Enter(invocationId, this, "SendRawRequest", new Dictionary<string, object>()
                {
                    {
                        "url",
                        url
                    },
                    {
                        "requestMethod",
                        requestMethod
                    },
                    {
                        "requestBody",
                        requestBody
                    },
                    {
                        "contentType",
                        contentType
                    }
                });
            }

            var request = default(HttpRequestMessage);
            var result = string.Empty;

            try
            {
                request = new HttpRequestMessage();
                request.Method = requestMethod;
                request.RequestUri = new Uri(url);
                request.Headers.Add("Accept", "application/json");
                request.Headers.Add("x-ms-version", "2013-06-01");

                if (requestBody != null && requestBody.Length > 0)
                {
                    request.Content = new StringContent(requestBody);
                    request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
                }

                var response = default(HttpResponseMessage);
                try
                {
                    response = await _httpClient.SendAsync(request).ConfigureAwait(false);
                    var statusCode = response.StatusCode;

                    // TODO: Handle async operations!
                    if (statusCode != HttpStatusCode.OK && statusCode != HttpStatusCode.Accepted && statusCode != HttpStatusCode.NoContent && statusCode != HttpStatusCode.Created)
                    {
                        if (statusCode == HttpStatusCode.InternalServerError && retryAutomatically)
                        {
                            // Retry the request
                            result = await SendRawRequestAsync(url, requestMethod, requestBody, contentType, false);
                        }
                        else if (statusCode == HttpStatusCode.NotFound)
                        {
                            // Ignore (i think!)
                            Debug.WriteLine(statusCode + ": " + url);
                        }
                        else
                        {
                            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                            throw new ApplicationException(content);
                        }
                    }

                    result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    if (TracingAdapter.IsEnabled)
                    {
                        TracingAdapter.Exit(invocationId, result);
                    }
                }
                finally
                {
                    if (response != null)
                        response.Dispose();
                }
            }
            finally
            {
                if (request != null)
                    request.Dispose();
            }

            if (result.StartsWith("<Error") && retryAutomatically)
            {
                // Error!
                result = await SendRawRequestAsync(url, requestMethod, requestBody, contentType, false);
            }
            else if (result.StartsWith("<Error"))
            {
                throw new ApplicationException(result);
            }

            return result;
        }

        public void Load()
        {
            if (SettingsService.CurrentSettings == null)
                return;

            Task.Run(() =>
            {
                // Load all runbooks
                var runbooksContent = SendRequest("runbooks", HttpMethod.Get);

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

                // Just load the runbooks first, so that we don't get 100+ message boxes with certificate errors
                Task.Run(() =>
                {
                    // Load all variables
                    var variablesContent = SendRequest("variables", HttpMethod.Get);

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

                            _backendContext.AddToVariables(new VariableModelProxy(variable, Context));
                        }

                        variablesRaw = null;
                    }
                });

                Task.Run(() =>
                {
                    // Load all credentials
                    var credentialsContent = SendRequest("credentials", HttpMethod.Get);

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

                Task.Run(() =>
                {
                    var connectionsContent = SendRequest("connections", HttpMethod.Get);

                    if (connectionsContent.Length > 0)
                    {
                        dynamic connectionsRaw = JObject.Parse(connectionsContent);
                        var connectionTypes = GetConnectionTypes();

                        foreach (var entry in connectionsRaw.value)
                        {
                            var connection = new Connection();
                            connection.Name = entry.name;
                            connection.Description = entry.properties.description;
                            connection.CreationTime = entry.properties.creationTime;
                            connection.LastModifiedTime = entry.properties.lastModifiedTime;

                            var connectionTypeName = entry.properties.connectionType.name.ToString();
                            var connectionType = connectionTypes.FirstOrDefault(type => type.Name.Equals(connectionTypeName, StringComparison.InvariantCultureIgnoreCase));
                            if (connectionType != null)
                                connection.ConnectionType = (ConnectionType)connectionType.Model;

                            // NOTE: Do we need to enumerate the parameters here? Don't think we do since they are no use
                            // to us until we want to edit a connection

                            _backendContext.AddToConnections(new ConnectionModelProxy(connection, Context));
                        }
                    }
                });

                Task.Run(() =>
                {
                    var modulesContent = SendRequest("modules", HttpMethod.Get);

                    if (modulesContent.Length > 0)
                    {
                        dynamic modulesRaw = JObject.Parse(modulesContent);

                        foreach (var entry in modulesRaw.value)
                        {
                            var module = new Module();
                            module.ModuleName = entry.name;
                            module.CreationTime = entry.properties.creationTime;
                            module.LastModifiedTime = entry.properties.lastModifiedTime;

                            _backendContext.AddToModules(new ModuleModelProxy(module, Context));
                        }
                    }
                });

                Task.Run(() =>
                {
                    // Load all schedules
                    var schedulesContent = SendRequest("schedules", HttpMethod.Get);

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
            });
        }

        public ResourceContainer GetStructure()
        {
            var resource = new ResourceContainer(_connectionData.Name, this, IconsDescription.Cloud);
            resource.Context = _backendContext;
            resource.IsExpanded = true;
            resource.Title = _connectionData.Name;

            // Runbooks
            var runbooks = new ResourceContainer("Runbooks", new Folder("Runbooks"), IconsDescription.Folder);
            runbooks.Context = _backendContext;
            runbooks.Items = _backendContext.Tags;
            runbooks.IsExpanded = true;
            resource.Items.Add(runbooks);

            // Connections
            var connections = new ResourceContainer("Connections", new Folder("Connections"), IconsDescription.Folder);
            connections.Context = _backendContext;
            connections.Items = _backendContext.Connections;
            resource.Items.Add(connections);

            // Credentials
            var credentials = new ResourceContainer("Credentials", new Folder("Credentials"), IconsDescription.Folder);
            credentials.Context = _backendContext;
            credentials.Items = _backendContext.Credentials;
            resource.Items.Add(credentials);

            // Modules
            var modules = new ResourceContainer("Modules", new Folder("Modules"), IconsDescription.Folder);
            modules.Context = _backendContext;
            modules.Items = _backendContext.Modules;
            resource.Items.Add(modules);

            // Schedules
            var schedules = new ResourceContainer("Schedules", new Folder("Schedules"), IconsDescription.Folder);
            schedules.Context = _backendContext;
            schedules.Items = _backendContext.Schedules;
            resource.Items.Add(schedules);

            // Variables
            var variables = new ResourceContainer("Variables", new Folder("Variables"), IconsDescription.Folder);
            variables.Context = _backendContext;
            variables.Items = _backendContext.Variables;
            resource.Items.Add(variables);

            return resource;
        }

        #endregion

        #region Runbooks

        public async Task<bool> CheckIn(RunbookModelProxy runbook)
        {
            var invocationId = string.Empty;

            if (TracingAdapter.IsEnabled)
            {
                invocationId = TracingAdapter.NextInvocationId.ToString();
                TracingAdapter.Enter(invocationId, this, "CheckIn", new Dictionary<string, object>()
                {
                    {
                        "runbook",
                        runbook
                    }
                });
            }

            // Publish the draft runbook
            await SendRequestAsync("runbooks/" + runbook.RunbookName.ToUrlSafeString() + "/draft/publish", HttpMethod.Post, "").ConfigureAwait(false);

            // Move the draft to published
            runbook.PublishedRunbookVersionID = runbook.DraftRunbookVersionID;
            runbook.DraftRunbookVersionID = null;

            if (TracingAdapter.IsEnabled)
            {
                TracingAdapter.Exit(invocationId, null);
            }

            return true;
        }

        public async Task<bool> CheckOut(RunbookViewModel runbook)
        {
            var invocationId = string.Empty;

            if (TracingAdapter.IsEnabled)
            {
                invocationId = TracingAdapter.NextInvocationId.ToString();
                TracingAdapter.Enter(invocationId, this, "CheckOut", new Dictionary<string, object>()
                {
                    {
                        "runbook",
                        runbook
                    }
                });
            }

            // Check out the runbok
            await SendRequestAsync("runbooks/" + runbook.Runbook.RunbookName.ToUrlSafeString() + "/draft/content", HttpMethod.Put, "").ConfigureAwait(false);

            // Notify SMA Studio that we have a draft and download the content
            runbook.Runbook.DraftRunbookVersionID = Guid.NewGuid();
            runbook.GetContent(RunbookType.Draft, true);

            if (TracingAdapter.IsEnabled)
            {
                TracingAdapter.Exit(invocationId, null);
            }

            return true;
        }

        private async Task<OperationResult> SaveAzureRunbook(IViewModel viewModel)
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

                var runbookData = await SendRequestAsync("runbooks/" + runbook.RunbookName.ToUrlSafeString(), HttpMethod.Put, JsonConvert.SerializeObject(json), "application/json").ConfigureAwait(false);

                if (runbookData.Length > 0)
                {
                    runbook.RunbookID = Guid.NewGuid();
                }
            }

            // Update the runbook
            var result = await SendRequestAsync("runbooks/" + runbook.RunbookName.ToUrlSafeString() + "/draft/content", HttpMethod.Put, viewModel.Content, "text/powershell").ConfigureAwait(false);

            //if (!String.IsNullOrEmpty(result))
            //{
            //    return await GetOperationResultAsync(result);
            //}

            // Reset the unsaved changes flag
            viewModel.UnsavedChanges = false;

            return new OperationResult
            {
                Status = OperationStatus.Succeeded,
                HttpStatusCode = HttpStatusCode.OK
            };
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
                SendRequest("jobs/" + jobGuid, HttpMethod.Put, json, "application/json");
            }
            catch (WebException ex)
            {
                //if (ex.Status == WebExceptionStatus.ProtocolError && (ex.Response as HttpWebResponse).StatusCode == HttpStatusCode.BadRequest)
                //    MessageBox.Show("A job is already running, please wait for that to complete and then try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                //else
                //    MessageBox.Show("An unknown error occurred when trying to start the runbook: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                throw new ApplicationException("An error occurred when starting the runbook, please refer to the output.", ex);
            }

            return jobGuid;
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
                var result = SendRequest("runbooks/" + runbookProxy.RunbookName.ToUrlSafeString() + "/draft/testJob", HttpMethod.Put, json, "application/json");
            }
            catch (WebException ex)
            {
                throw new ApplicationException("An error occurred when testing the runbook, please refer to the output.", ex);
            }

            return jobGuid;
        }

        public string GetContent(string url)
        {
            return SendRawRequest(url, HttpMethod.Get);
        }

        public async Task<string> GetContentAsync(string url)
        {
            return await SendRawRequestAsync(url, HttpMethod.Get);
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

        #endregion

        #region Jobs

        public async Task PauseExecution(RunbookModelProxy runbook, bool isDraft = false)
        {
            if (isDraft)
                await SendRequestAsync("runbooks/" + runbook.RunbookName.ToUrlSafeString() + "/draft/testJob/suspend", HttpMethod.Post, "", "0").ConfigureAwait(false);
            else
                await SendRequestAsync("jobs/" + runbook.JobID + "/suspend", HttpMethod.Post, "", "0").ConfigureAwait(false);
        }

        public async Task ResumeExecution(RunbookModelProxy runbook, bool isDraft = false)
        {
            if (isDraft)
                await SendRequestAsync("runbooks/" + runbook.RunbookName.ToUrlSafeString() + "/draft/testJob/resume", HttpMethod.Post, "", "0").ConfigureAwait(false);
            else
                await SendRequestAsync("jobs/" + runbook.JobID + "/resume", HttpMethod.Post, "", "0").ConfigureAwait(false);
        }

        public async Task StopExecution(RunbookModelProxy runbook, bool isDraft = false)
        {
            if (isDraft)
                await SendRequestAsync("runbooks/" + runbook.RunbookName.ToUrlSafeString() + "/draft/testJob/stop", HttpMethod.Post, "", "0").ConfigureAwait(false);
            else
                await SendRequestAsync("jobs/" + runbook.JobID + "/stop", HttpMethod.Post, "", "0").ConfigureAwait(false);
        }

        public async Task<bool> CheckRunningJobs(RunbookModelProxy runbook, bool checkDraft)
        {
            var result = string.Empty;

            if (checkDraft)
            {
                result = await SendRequestAsync("runbooks/" + runbook.RunbookName.ToUrlSafeString() + "/draft/testJob", HttpMethod.Get).ConfigureAwait(false);
            }
            else if (runbook.JobID != Guid.Empty)
            {
                result = await SendRequestAsync("jobs/" + runbook.JobID, HttpMethod.Get).ConfigureAwait(false);
            }

            if (result.Length > 0)
            {
                dynamic jobRaw = JObject.Parse(result);
                var status = jobRaw.status.ToString();

                if (_completionStatusList.Contains(status))
                    return false;
            }
            else
                return false;

            return true;
        }

        public async Task<JobModelProxy> GetJobInformationAsync(Guid jobId)
        {
            var result = await SendRequestAsync("jobs/" + jobId, HttpMethod.Get);
            dynamic jobRaw = JObject.Parse(result);

            var model = new JobModelProxy(new Job(), Context);
            model.Result.Add(new JobOutput
            {
                StreamText = jobRaw.properties.exception,
                StreamTime = DateTime.Now,
                StreamTypeName = jobRaw.properties.exception != null ? "Error" : "Information"
            });

            return model;
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
            var result = SendRequest("runbooks/" + runbook.RunbookName.ToUrlSafeString() + "/draft/testJob", HttpMethod.Get);

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
            job.JobException = jsonJob.exception;

            var jobModelProxy = new JobModelProxy(job, Context);

            foreach (var param in jsonJob.parameters)
            {
                //jobModelProxy.Parameters.Add(param)
            }

            var output = string.Empty;

            try
            {
                output = SendRequest("runbooks/" + runbook.RunbookName.ToUrlSafeString() + "/draft/testJob/streams", HttpMethod.Get);
                jobModelProxy = ParseJobStreams(jobModelProxy, output);
            }
            catch (WebException)
            {
                // The job haven't been started yet, ignore this.
            }

            return jobModelProxy;
        }

        private JobModelProxy GetPublishedJobDetails(Guid jobId)
        {
            var result = SendRequest("jobs/" + jobId, HttpMethod.Get);

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

            var output = SendRequest("jobs/" + jobId + "/streams", HttpMethod.Get);
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
            var result = SendRequest("jobs", HttpMethod.Get);
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

        #endregion

        #region Credentials

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
                SendRequest("credentials/" + credential.Name.ToUrlSafeString(), HttpMethod.Put, JsonConvert.SerializeObject(dict), "application/json");
            }
            else
            {
                SendRequest("credentials/" + credential.Name.ToUrlSafeString(), new HttpMethod("PATCH"), JsonConvert.SerializeObject(dict), "application/json");
            }

            viewModel.UnsavedChanges = false;
        }

        #endregion

        #region Connections

        private void SaveAzureConnection(IViewModel viewModel)
        {
            var connection = viewModel.Model as ConnectionModelProxy;

            var dict = new Dictionary<string, object>();
            var properties = new Dictionary<string, object>();
            properties.Add("description", connection.Description);

            var connectionType = new Dictionary<string, object>();
            connectionType.Add("name", (connection.ConnectionType as ConnectionType).Name);
            properties.Add("connectionType", connectionType);

            var fieldDefinitionValues = new Dictionary<string, object>();

            foreach (var field in (connection.ConnectionFieldValues as List<ConnectionFieldValue>))
            {
                fieldDefinitionValues.Add(field.ConnectionFieldName, field.Value);
            }

            properties.Add("fieldDefinitionValues", fieldDefinitionValues);
            dict.Add("name", connection.Name);
            dict.Add("properties", properties);

            SendRequest("connections/" + connection.Name.ToUrlSafeString(), HttpMethod.Put, JsonConvert.SerializeObject(dict), "application/json");

            viewModel.UnsavedChanges = false;
        }

        public IList<ConnectionTypeModelProxy> GetConnectionTypes()
        {
            var result = new List<ConnectionTypeModelProxy>();
            var connectionTypesContent = SendRequest("connectionTypes", HttpMethod.Get);

            if (connectionTypesContent.Length == 0)
                return null;

            dynamic connectionTypesRaw = JObject.Parse(connectionTypesContent);

            foreach (var entry in connectionTypesRaw.value)
            {
                var item = new ConnectionType();
                item.Name = entry.name;
                item.CreationTime = entry.properties.creationTime;
                item.LastModifiedTime = entry.properties.lastModifiedTime;
                item.ConnectionFields = new List<ConnectionField>();

                foreach (var def in entry.properties.fieldDefinitions)
                {
                    var field = new ConnectionField();
                    field.Name = def.Name;
                    field.IsEncrypted = def.Value.isEncrypted;
                    field.IsOptional = def.Value.isOptional;
                    field.Type = def.Value.type;

                    item.ConnectionFields.Add(field);
                }

                result.Add(new ConnectionTypeModelProxy(item, this.Context));
            }

            return result;
        }

        public ConnectionModelProxy GetConnectionDetails(ConnectionModelProxy connection)
        {
            var connectionContent = SendRequest("connections/" + connection.Name, HttpMethod.Get);

            if (connectionContent.Length == 0)
                return null;

            dynamic connectionRaw = JObject.Parse(connectionContent);

            var connectionType = (ConnectionType)connection.ConnectionType;

            foreach (var entry in connectionRaw.properties.fieldDefinitionValues)
            {
                var field = connectionType.ConnectionFields.FirstOrDefault(item => item.Name.Equals(entry.Name, StringComparison.InvariantCultureIgnoreCase));

                var fieldValue = new ConnectionFieldValue();
                fieldValue.Connection = (Connection)connection.Model;
                fieldValue.ConnectionFieldName = field.Name;
                fieldValue.ConnectionName = fieldValue.Connection.Name;
                fieldValue.ConnectionTypeName = connectionType.Name;
                fieldValue.IsEncrypted = field.IsEncrypted;
                fieldValue.IsOptional = field.IsOptional;
                fieldValue.Value = entry.Value;
                fieldValue.Type = field.Type;

                (connection.ConnectionFieldValues as List<ConnectionFieldValue>).Add(fieldValue);
            }

            return connection;
        }

        #endregion

        #region Modules

        private void SaveAzureModule(IViewModel viewModel)
        {
            var module = viewModel.Model as ModuleModelProxy;

            var dict = new Dictionary<string, object>();
            var properties = new Dictionary<string, object>();

            var contentLink = new Dictionary<string, object>();
            contentLink.Add("uri", module.ModuleUrl);
            contentLink.Add("version", module.ModuleVersion);
            properties.Add("contentLink", contentLink);

            dict.Add("properties", properties);

            SendRequest("modules/" + module.ModuleName.ToUrlSafeString(), HttpMethod.Put, JsonConvert.SerializeObject(dict), "application/json");

            viewModel.UnsavedChanges = false;
        }

        #endregion

        #region Schedules

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
                SendRequest("schedules/" + schedule.Name.ToUrlSafeString(), HttpMethod.Put, JsonConvert.SerializeObject(dict), "application/json");
            }
            else
            {
                SendRequest("schedules/" + schedule.Name.ToUrlSafeString(), new HttpMethod("PATCH"), JsonConvert.SerializeObject(dict), "application/json");
            }

            viewModel.UnsavedChanges = false;
        }

        #endregion

        #region Variables

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
                SendRequest("variables/" + variable.Name.ToUrlSafeString(), HttpMethod.Put, JsonConvert.SerializeObject(dict), "application/json");
            }
            else
            {
                SendRequest("variables/" + variable.Name.ToUrlSafeString(), new HttpMethod("PATCH"), JsonConvert.SerializeObject(dict), "application/json");
            }

            viewModel.UnsavedChanges = false;
        }

        #endregion

        // TODO: Need rewrite
        #region Operations

        /// <summary>
        /// Retrieves the operation result from the Azure webservice
        /// </summary>
        /// <param name="requestId">Request to check status on</param>
        /// <returns>Operation result</returns>
        private async Task<OperationResult> GetOperationResultAsync(string requestUrl)
        {
            var invocationId = string.Empty;

            if (TracingAdapter.IsEnabled)
            {
                invocationId = TracingAdapter.NextInvocationId.ToString();
                TracingAdapter.Enter(invocationId, this, "GetOperationResultAsync", new Dictionary<string, object>()
                {
                    {
                        "requestUrl",
                        requestUrl
                    }
                });
            }

            var operationStatus = new OperationResult();

            if (requestUrl == null)
                throw new ArgumentNullException("requestUrl");

            //var requestUrl = String.Format("{0}/operations/{1}", AzureBaseUrl, requestId.Replace(" ", "%20"));
            requestUrl = requestUrl.Replace("https://oaas.azure-automation.net/subscriptions/", AzureBaseUrl);
            requestUrl = requestUrl.Replace("resources/~/", "resources/automation/~/");

            var asyncResponse = default(WebResponse);
            var asyncRequest = (HttpWebRequest)WebRequest.Create(requestUrl);
            asyncRequest.Method = "GET";
            asyncRequest.Accept = "application/json";
            asyncRequest.Headers.Add("x-ms-version", "2013-06-01");
            asyncRequest.ClientCertificates.Add(_certificate);
            asyncRequest.Timeout = 2000;

            asyncResponse = asyncRequest.GetResponse();

            using (var responseStream = new StreamReader(asyncResponse.GetResponseStream()))
            {
                var statusCode = (asyncResponse as HttpWebResponse).StatusCode;

                var content = await responseStream.ReadToEndAsync();//.ConfigureAwait(false);
                if (content != null && content.Length > 0)
                {
                    var document = XDocument.Parse(content);

                    var opElement = document.Element(XName.Get("Operation", AzureXNS));
                    if (opElement != null)
                    {
                        var statusElement = opElement.Element(XName.Get("Status", AzureXNS));
                        if (statusElement != null)
                            operationStatus.Status = (OperationStatus)Enum.Parse(typeof(OperationStatus), statusElement.Value, true);

                        var httpStatusElement = opElement.Element(XName.Get("HttpStatusCode", AzureXNS));
                        if (httpStatusElement != null)
                            operationStatus.HttpStatusCode = (HttpStatusCode)Enum.Parse(typeof(HttpStatusCode), httpStatusElement.Value, true);

                        var errorElement = opElement.Element(XName.Get("Error", AzureXNS));
                        if (errorElement != null)
                        {
                            var codeElement = errorElement.Element(XName.Get("Code", AzureXNS));
                            if (codeElement != null)
                                operationStatus.ErrorCode = codeElement.Value;

                            var messageElement = errorElement.Element(XName.Get("Message", AzureXNS));
                            if (messageElement != null)
                                operationStatus.ErrorMessage = messageElement.Value;
                        }
                    }
                }
                else
                {
                    if (statusCode == HttpStatusCode.NotFound || statusCode == HttpStatusCode.BadRequest)
                        operationStatus.Status = OperationStatus.Failed;
                    else if (statusCode == HttpStatusCode.Created || statusCode == HttpStatusCode.NoContent || statusCode == HttpStatusCode.OK)
                        operationStatus.Status = OperationStatus.Succeeded;

                    operationStatus.HttpStatusCode = statusCode;
                }
            }

            if (asyncResponse.Headers["x-ms-request-id"] != null)
            {
                operationStatus.RequestUrl = asyncResponse.Headers["x-ms-request-id"];
            }

            if (asyncResponse != null)
                asyncResponse.Dispose();

            if (TracingAdapter.IsEnabled)
            {
                TracingAdapter.Exit(invocationId, operationStatus);
            }

            return operationStatus;
        }

        #endregion
    }
}
