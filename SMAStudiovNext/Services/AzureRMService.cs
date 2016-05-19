using Caliburn.Micro;
using Gemini.Framework;
using Gemini.Framework.Commands;
using Gemini.Framework.Services;
using Gemini.Modules.Output;
using Hyak.Common;
using Microsoft.Azure;
using Microsoft.Azure.Management.Automation;
using Microsoft.Azure.Management.Automation.Models;
using Newtonsoft.Json;
using SMAStudiovNext.Core;
using SMAStudiovNext.Core.Net;
using SMAStudiovNext.Icons;
using SMAStudiovNext.Models;
using SMAStudiovNext.Modules.WindowRunbook.Editor.Completion;
using SMAStudiovNext.Modules.WindowRunbook.ViewModels;
using SMAStudiovNext.SMA;
using SMAStudiovNext.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SMAStudiovNext.Services
{
    public class AzureRMService : IBackendService
    {
        private static int TIMEOUT_MS = 30000;
        private const string AzureBaseUrl = "https://management.core.windows.net/";
        private const string AzureResourceUrl = "subscriptions/{0}/resourceGroups/{1}/providers/Microsoft.Automation/automationAccounts/{2}/{3}?api-version=2015-10-31";
        private const string AzureXNS = "http://schemas.microsoft.com/windowsazure";
        private const string AzureTokenUrl = "https://login.microsoftonline.com/{0}/oauth2/token?api-version=1.0";
        private const string AzureRedirectUrl = "urn:ietf:wg:oauth:2.0:oob";
        private const string AutoStudioTagName = "AutoStudio";

        private readonly IBackendContext _backendContext;
        private readonly BackendConnection _connectionData;
        private readonly IDictionary<Guid, List<AccessToken>> _tokens;
        private readonly WebRequestHandler _webRequestHandler;
        private readonly IOutput _output;
        private readonly List<string> _completionStatusList = new List<string> { "Completed", "Failed", "Stopped" };

        private TokenCloudCredentials _accessToken;
        private AutomationManagementClient _client;
        private X509Certificate2 _certificate = null;

        private Dictionary<Guid, Microsoft.Azure.Management.Automation.Models.Job> _jobCache;
        private Dictionary<Guid, Microsoft.Azure.Management.Automation.Models.TestJob> _testJobCache;

        public IBackendContext Context
        {
            get { return _backendContext; }
        }

        public AzureRMService(IBackendContext context, BackendConnection connectionData)
        {
            _backendContext = context;
            _connectionData = connectionData;
            _tokens = new Dictionary<Guid, List<AccessToken>>();
            _output = IoC.Get<IOutput>();
            _jobCache = new Dictionary<Guid, Microsoft.Azure.Management.Automation.Models.Job>();
            _testJobCache = new Dictionary<Guid, TestJob>();

            // We need to fetch a token for the backend connection
            InitializeAsync().Wait();

            /*_certificate = CertificateManager.FindCertificate(_connectionData.AzureCertificateThumbprint);
            _webRequestHandler = new WebRequestHandler();
            _webRequestHandler.ClientCertificates.Add(_certificate);
            _client.HttpClient = new HttpClient(_webRequestHandler);*/
        }

        private async Task InitializeAsync()
        {
            await RefreshTokenAsync();

            _client = new AutomationManagementClient(_accessToken);

            // We need the location of the azure automation account for later use
            var account = _client.AutomationAccounts.Get(_connectionData.AzureRMGroupName, _connectionData.AzureAutomationAccount);
            _connectionData.AzureRMLocation = account.AutomationAccount.Location;
        }

        private async Task RefreshTokenAsync()
        {
            var token = await GetTokenAsync(Guid.Parse(_connectionData.AzureRMTenantId));
            
            if (token == null)
                throw new InvalidOperationException("Sorry, a token could not be accquired. Please verify your Service Principal.");
            
            if (_accessToken == null)
                _accessToken = new TokenCloudCredentials(_connectionData.AzureSubscriptionId, token.access_token);
        }

        public async Task<bool> CheckIn(RunbookModelProxy runbook)
        {
            RunbookDraftPublishParameters publishParams = new RunbookDraftPublishParameters
            {
                Name = runbook.RunbookName,
                PublishedBy = "Automation Studio, by: " + System.Security.Principal.WindowsIdentity.GetCurrent().Name
            };

            CancellationTokenSource cts = new CancellationTokenSource();
            cts.CancelAfter(TIMEOUT_MS);

            LongRunningOperationResultResponse resultResponse = await _client.RunbookDraft.PublishAsync(_connectionData.AzureRMGroupName, _connectionData.AzureAutomationAccount, publishParams, cts.Token);

            // Move the draft to published
            runbook.PublishedRunbookVersionID = runbook.DraftRunbookVersionID;
            runbook.DraftRunbookVersionID = null;

            return true;
        }

        public async Task<bool> CheckOut(RunbookViewModel runbook)
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            cts.CancelAfter(TIMEOUT_MS);

            RunbookGetResponse response = await _client.Runbooks.GetAsync(_connectionData.AzureRMGroupName, _connectionData.AzureAutomationAccount, runbook.Runbook.RunbookName, cts.Token);

            if (response.Runbook.Properties.State != "Published")
                return false;

            cts = new CancellationTokenSource();
            cts.CancelAfter(TIMEOUT_MS);

            RunbookContentResponse runbookContentResponse = await _client.Runbooks.ContentAsync(_connectionData.AzureRMGroupName, _connectionData.AzureAutomationAccount, runbook.Runbook.RunbookName, cts.Token);
            
            // Create draft properties
            RunbookCreateOrUpdateDraftParameters draftParams = new RunbookCreateOrUpdateDraftParameters();
            draftParams.Properties = new RunbookCreateOrUpdateDraftProperties();
            draftParams.Properties.Description = response.Runbook.Properties.Description;
            draftParams.Properties.LogProgress = response.Runbook.Properties.LogProgress;
            draftParams.Properties.LogVerbose = response.Runbook.Properties.LogVerbose;
            draftParams.Properties.RunbookType = response.Runbook.Properties.RunbookType;
            draftParams.Properties.Draft = new RunbookDraft();
            draftParams.Tags = response.Runbook.Tags;
            draftParams.Name = runbook.Runbook.RunbookName;
            draftParams.Location = _connectionData.AzureRMLocation;

            cts = new CancellationTokenSource();
            cts.CancelAfter(TIMEOUT_MS);

            await _client.Runbooks.CreateOrUpdateWithDraftAsync(_connectionData.AzureRMGroupName, _connectionData.AzureAutomationAccount, draftParams, cts.Token);
            RunbookDraftUpdateParameters draftUpdateParams = new RunbookDraftUpdateParameters()
            {
                Name = runbook.Runbook.RunbookName,
                Stream = runbookContentResponse.Stream.ToString()
            };
            cts = new CancellationTokenSource();
            cts.CancelAfter(TIMEOUT_MS);

            await _client.RunbookDraft.UpdateAsync(_connectionData.AzureRMGroupName, _connectionData.AzureAutomationAccount, draftUpdateParams, cts.Token);

            return true;
        }

        public async Task<bool> CheckRunningJobs(RunbookModelProxy runbook, bool checkDraft)
        {
            try
            {
                if (checkDraft)
                {
                    // Test job
                    var response = await _client.TestJobs.GetAsync(_connectionData.AzureRMGroupName, _connectionData.AzureAutomationAccount, runbook.RunbookName);

                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        if (_completionStatusList.Contains(response.TestJob.Status))
                            return false;
                    }
                }
                else
                {
                    var response = await _client.Jobs.GetAsync(_connectionData.AzureRMGroupName, _connectionData.AzureAutomationAccount, runbook.JobID);

                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        if (_completionStatusList.Contains(response.Job.Properties.Status))
                            return false;
                    }
                }
            }
            catch (CloudException)
            {
                return false;
            }

            return true;
        }

        public bool Delete(ModelProxyBase model)
        {
            if (model is RunbookModelProxy)
            {
                _client.Runbooks.Delete(_connectionData.AzureRMGroupName, _connectionData.AzureAutomationAccount, (model as RunbookModelProxy).RunbookName);
            }
            else if (model is VariableModelProxy)
            {
                _client.Variables.Delete(_connectionData.AzureRMGroupName, _connectionData.AzureAutomationAccount, (model as VariableModelProxy).Name);
            }
            else if (model is ConnectionModelProxy)
            {
                _client.Connections.Delete(_connectionData.AzureRMGroupName, _connectionData.AzureAutomationAccount, (model as ConnectionModelProxy).Name);
            }
            else if (model is CredentialModelProxy)
            {
                _client.PsCredentials.Delete(_connectionData.AzureRMGroupName, _connectionData.AzureAutomationAccount, (model as CredentialModelProxy).Name);
            }
            else if (model is ModuleModelProxy)
            {
                _client.Modules.Delete(_connectionData.AzureRMGroupName, _connectionData.AzureAutomationAccount, (model as ModuleModelProxy).ModuleName);
            }

            return true;
        }

        public string GetBackendUrl(RunbookType runbookType, RunbookModelProxy runbook)
        {
            // Azure RM, apart from Classic, only requires the runbook name when fetching the content.
            // That's why we only return runbook name here.
            return runbookType + "|" + runbook.RunbookName;
        }

        public ConnectionModelProxy GetConnectionDetails(ConnectionModelProxy connection)
        {
            var response = _client.Connections.Get(_connectionData.AzureRMGroupName, _connectionData.AzureAutomationAccount, connection.Name);

            if (response.Connection != null)
            {
                var connectionType = (Vendor.Azure.ConnectionType)connection.ConnectionType;

                foreach (var entry in response.Connection.Properties.FieldDefinitionValues)
                {
                    var field = connectionType.ConnectionFields.FirstOrDefault(item => item.Name.Equals(entry.Key, StringComparison.InvariantCultureIgnoreCase));

                    var fieldValue = new Vendor.Azure.ConnectionFieldValue();
                    fieldValue.Connection = (Vendor.Azure.Connection)connection.Model;
                    fieldValue.ConnectionFieldName = field.Name;
                    fieldValue.ConnectionName = fieldValue.Connection.Name;
                    fieldValue.ConnectionTypeName = connectionType.Name;
                    fieldValue.IsEncrypted = field.IsEncrypted;
                    fieldValue.IsOptional = field.IsOptional;
                    fieldValue.Value = entry.Value;
                    fieldValue.Type = field.Type;

                    (connection.ConnectionFieldValues as List<Vendor.Azure.ConnectionFieldValue>).Add(fieldValue);
                }
            }

            return connection;
        }

        public IList<ConnectionTypeModelProxy> GetConnectionTypes()
        {
            var response = _client.ConnectionTypes.List(_connectionData.AzureRMGroupName, _connectionData.AzureAutomationAccount);
            var result = new List<ConnectionTypeModelProxy>();

            result.AddRange(response.ConnectionTypes.Select(c => new ConnectionTypeModelProxy(new Vendor.Azure.ConnectionType
            {
                Name = c.Name,
                CreationTime = c.Properties.CreationTime.DateTime,
                LastModifiedTime = c.Properties.LastModifiedTime.DateTime,
                ConnectionFields = c.Properties.FieldDefinitions.Keys.Select(k => new Vendor.Azure.ConnectionField
                {
                    Name = k,
                    Type = c.Properties.FieldDefinitions[k].Type,
                    IsEncrypted = c.Properties.FieldDefinitions[k].IsEncrypted,
                    IsOptional = c.Properties.FieldDefinitions[k].IsOptional
                }).ToList()
            }, Context)).ToList());

            while (response.NextLink != null)
            {
                response = _client.ConnectionTypes.ListNext(response.NextLink);

                result.AddRange(response.ConnectionTypes.Select(c => new ConnectionTypeModelProxy(new Vendor.Azure.ConnectionType
                {
                    Name = c.Name,
                    CreationTime = c.Properties.CreationTime.DateTime,
                    LastModifiedTime = c.Properties.LastModifiedTime.DateTime,
                    ConnectionFields = c.Properties.FieldDefinitions.Keys.Select(k => new Vendor.Azure.ConnectionField
                    {
                        Name = k,
                        Type = c.Properties.FieldDefinitions[k].Type,
                        IsEncrypted = c.Properties.FieldDefinitions[k].IsEncrypted,
                        IsOptional = c.Properties.FieldDefinitions[k].IsOptional
                    }).ToList()
                }, Context)).ToList());
            }

            return result;
        }

        public string GetContent(string url)
        {
            var parts = url.Split('|');
            
            switch (parts[0])
            {
                case "Draft":
                    return _client.RunbookDraft.Content(_connectionData.AzureRMGroupName, _connectionData.AzureAutomationAccount, parts[1]).Stream;
                default:
                    return _client.Runbooks.Content(_connectionData.AzureRMGroupName, _connectionData.AzureAutomationAccount, parts[1]).Stream;
            }
        }

        public async Task<string> GetContentAsync(string url)
        {
            var parts = url.Split('|');

            switch (parts[0])
            {
                case "Draft":
                    var draft = await _client.RunbookDraft.ContentAsync(_connectionData.AzureRMGroupName, _connectionData.AzureAutomationAccount, parts[1]);

                    return draft.Stream;
                default:
                    var published = await _client.Runbooks.ContentAsync(_connectionData.AzureRMGroupName, _connectionData.AzureAutomationAccount, parts[1]);

                    return published.Stream;
            }
        }

        public JobModelProxy GetJobDetails(Guid jobId)
        {
            return GetPublishedJobDetails(jobId);
        }

        public JobModelProxy GetJobDetails(RunbookModelProxy runbook)
        {
            if (runbook.IsTestRun)
                return GetDraftJobDetails(runbook);

            if (runbook.JobID != Guid.Empty)
                return GetPublishedJobDetails(runbook.JobID);

            return new JobModelProxy(new Vendor.Azure.Job(), Context);
        }

        private JobModelProxy GetPublishedJobDetails(Guid jobId)
        {
            var job = _client.Jobs.Get(_connectionData.AzureRMGroupName, _connectionData.AzureAutomationAccount, jobId);

            if (job.StatusCode != System.Net.HttpStatusCode.OK)
                return null;

            var jobModel = new JobModelProxy(new Vendor.Azure.Job
            {
                JobID = job.Job.Properties.JobId,
                Id = job.Job.Id,
                WarningCount = 0,
                ErrorCount = 0,
                JobException = job.Job.Properties.Exception,
                CreationTime = job.Job.Properties.CreationTime.DateTime,
                EndTime = (job.Job.Properties.EndTime.HasValue ? job.Job.Properties.EndTime.Value.DateTime : default(DateTime?)),
                JobStatus = job.Job.Properties.Status,
                JobStatusDeteails = job.Job.Properties.StatusDetails,
                LastModifiedTime = job.Job.Properties.LastModifiedTime.DateTime,
                StartTime = (job.Job.Properties.StartTime.HasValue ? job.Job.Properties.StartTime.Value.DateTime : default(DateTime?))
            }, Context);

            var jobStreamParameters = new JobStreamListParameters();
            jobStreamParameters.StreamType = "Any";

            var streams = default(JobStreamListResponse);
            do
            {
                if (streams != null)
                    streams = _client.JobStreams.ListNext(streams.NextLink);
                else
                    streams = _client.JobStreams.List(_connectionData.AzureRMGroupName, _connectionData.AzureAutomationAccount, jobId, jobStreamParameters);

                if (streams.StatusCode != System.Net.HttpStatusCode.OK)
                    return jobModel;

                var output = streams.JobStreams.Select(s => new JobOutput
                {
                    JobID = jobModel.JobID,
                    RunbookVersionID = Guid.Empty,
                    StreamText = s.Properties.StreamText,
                    StreamTime = s.Properties.Time.DateTime,
                    StreamTypeName = s.Properties.StreamType
                }).ToList();

                foreach (var o in output)
                    jobModel.Result.Add(o);
            }
            while (streams.NextLink != null);

            return jobModel;
        }

        private JobModelProxy GetDraftJobDetails(RunbookModelProxy runbook)
        {
            var job = _client.TestJobs.Get(_connectionData.AzureRMGroupName, _connectionData.AzureAutomationAccount, runbook.RunbookName);

            if (job.StatusCode != System.Net.HttpStatusCode.OK)
                return null;

            var jobModel = new JobModelProxy(new Vendor.Azure.Job
            {
                JobID = Guid.NewGuid(),
                Id = string.Empty,
                WarningCount = 0,
                ErrorCount = 0,
                JobException = job.TestJob.Exception,
                CreationTime = job.TestJob.CreationTime.DateTime,
                EndTime = (job.TestJob.EndTime != null ? job.TestJob.EndTime.DateTime : default(DateTime?)),
                JobStatus = job.TestJob.Status,
                JobStatusDeteails = job.TestJob.StatusDetails,
                LastModifiedTime = job.TestJob.LastModifiedTime.DateTime,
                StartTime = (job.TestJob.StartTime != null ? job.TestJob.StartTime.DateTime : default(DateTime?))
            }, Context);

            var jobStreamParameters = new JobStreamListParameters();
            jobStreamParameters.StreamType = "Any";

            var streams = default(JobStreamListResponse);
            do
            {
                if (streams != null)
                    streams = _client.JobStreams.ListNext(streams.NextLink);
                else
                    streams = _client.JobStreams.ListTestJobStreams(_connectionData.AzureRMGroupName, _connectionData.AzureAutomationAccount, runbook.RunbookName, jobStreamParameters);

                if (streams.StatusCode != System.Net.HttpStatusCode.OK)
                    return jobModel;

                var output = streams.JobStreams.Select(s => new JobOutput
                {
                    JobID = jobModel.JobID,
                    RunbookVersionID = runbook.DraftRunbookVersionID.Value,
                    StreamText = s.Properties.Summary,
                    StreamTime = s.Properties.Time.DateTime,
                    StreamTypeName = s.Properties.StreamType
                }).ToList();

                foreach (var o in output)
                    jobModel.Result.Add(o);
            }
            while (streams.NextLink != null);

            return jobModel;
        }

        public Task<JobModelProxy> GetJobInformationAsync(Guid jobId)
        {
            throw new NotImplementedException();
        }

        public IList<JobModelProxy> GetJobs(Guid runbookVersionId)
        {
            // Since Azure are so nice, not providing any IDs for our runbooks and instead
            // using the name to identify the runbooks (as contrary to SMA which uses GUIDs),
            // we need to query our backend context to retrieve the correct runbook based on
            // our genereated GUID
            var resource = Context.Runbooks.FirstOrDefault(r => (r.Tag as RunbookModelProxy).PublishedRunbookVersionID == runbookVersionId);

            if (resource == null) // no runbook found?
                return new List<JobModelProxy>();

            var runbook = resource.Tag as RunbookModelProxy;

            var jobList = new JobListParameters();
            jobList.RunbookName = runbook.RunbookName;

            var jobs = default(JobListResponse);
            var response = new List<JobModelProxy>();

            do
            {
                if (jobs != null)
                    jobs = _client.Jobs.ListNext(jobs.NextLink);
                else
                    jobs = _client.Jobs.List(_connectionData.AzureRMGroupName, _connectionData.AzureAutomationAccount, jobList);

                if (jobs.Jobs.Count > 0)
                {
                    var jobModels = jobs.Jobs.Select(j => new JobModelProxy(new Vendor.Azure.Job
                    {
                        JobID = j.Properties.JobId,
                        Id = j.Id,
                        WarningCount = 0,
                        ErrorCount = 0,
                        JobException = j.Properties.Exception,
                        CreationTime = j.Properties.CreationTime.DateTime,
                        EndTime = (j.Properties.EndTime.HasValue ? j.Properties.EndTime.Value.DateTime : default(DateTime?)),
                        JobStatus = j.Properties.Status,
                        JobStatusDeteails = j.Properties.StatusDetails,
                        LastModifiedTime = j.Properties.LastModifiedTime.DateTime,
                        StartTime = (j.Properties.StartTime.HasValue ? j.Properties.StartTime.Value.DateTime : default(DateTime?))
                    }, Context)).ToList();

                    response.AddRange(jobModels);
                }
            }
            while (jobs.NextLink != null);

            return response;
        }

        public IList<ICompletionEntry> GetParameters(RunbookViewModel runbookViewModel, KeywordCompletionData completionData)
        {
            throw new NotImplementedException();
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

        public void Load()
        {
            if (SettingsService.CurrentSettings == null)
                return;

            var runbooksResponse = _client.Runbooks.List(_connectionData.AzureRMGroupName, _connectionData.AzureAutomationAccount);

            if (runbooksResponse.Runbooks.Count > 0)
            {
                var runbooks = runbooksResponse.Runbooks.Select(r => new Vendor.Azure.Runbook
                {
                    Id = r.Id,
                    RunbookID = Guid.NewGuid(),
                    RunbookName = r.Name,
                    State = r.Properties.State,
                    Tags = r.Tags.ContainsKey(AutoStudioTagName) ? r.Tags[AutoStudioTagName] : string.Empty
                }).ToList();

                foreach (var runbook in runbooks)
                    _backendContext.AddToRunbooks(new RunbookModelProxy(runbook, _backendContext));

                while (runbooksResponse.NextLink != null)
                {
                    runbooksResponse = _client.Runbooks.ListNext(runbooksResponse.NextLink);

                    runbooks = runbooksResponse.Runbooks.Select(r => new Vendor.Azure.Runbook
                    {
                        Id = r.Id,
                        RunbookID = Guid.NewGuid(),
                        RunbookName = r.Name,
                        State = r.Properties.State,
                        Tags = r.Tags.ContainsKey(AutoStudioTagName) ? r.Tags[AutoStudioTagName] : string.Empty
                    }).ToList();

                    foreach (var runbook in runbooks)
                        _backendContext.AddToRunbooks(new RunbookModelProxy(runbook, _backendContext));
                }
            }

            var variablesResponse = _client.Variables.List(_connectionData.AzureRMGroupName, _connectionData.AzureAutomationAccount);

            if (variablesResponse.Variables.Count > 0)
            {
                var variables = variablesResponse.Variables.Select(v => new Vendor.Azure.Variable
                {
                    Id = v.Id,
                    VariableID = Guid.NewGuid(),
                    Value = v.Properties.Value,
                    Name = v.Name,
                    IsEncrypted = v.Properties.IsEncrypted
                });

                foreach (var variable in variables)
                    _backendContext.AddToVariables(new VariableModelProxy(variable, _backendContext));

                while (variablesResponse.NextLink != null)
                {
                    variablesResponse = _client.Variables.ListNext(variablesResponse.NextLink);

                    variables = variablesResponse.Variables.Select(v => new Vendor.Azure.Variable
                    {
                        Id = v.Id,
                        VariableID = Guid.NewGuid(),
                        Value = v.Properties.Value,
                        Name = v.Name,
                        IsEncrypted = v.Properties.IsEncrypted
                    });

                    foreach (var variable in variables)
                        _backendContext.AddToVariables(new VariableModelProxy(variable, _backendContext));
                }
            }

            var credentialsResponse = _client.PsCredentials.List(_connectionData.AzureRMGroupName, _connectionData.AzureAutomationAccount);

            if (credentialsResponse.Credentials.Count > 0)
            {
                var credentials = credentialsResponse.Credentials.Select(c => new Vendor.Azure.Credential
                {
                    Id = c.Id,
                    CredentialID = Guid.NewGuid(),
                    Name = c.Name,
                    UserName = c.Properties.UserName
                });

                foreach (var credential in credentials)
                    _backendContext.AddToCredentials(new CredentialModelProxy(credential, _backendContext));

                while (credentialsResponse.NextLink != null)
                {
                    credentialsResponse = _client.PsCredentials.ListNext(credentialsResponse.NextLink);

                    credentials = credentialsResponse.Credentials.Select(c => new Vendor.Azure.Credential
                    {
                        Id = c.Id,
                        CredentialID = Guid.NewGuid(),
                        Name = c.Name,
                        UserName = c.Properties.UserName
                    });

                    foreach (var credential in credentials)
                        _backendContext.AddToCredentials(new CredentialModelProxy(credential, _backendContext));
                }
            }

            var connectionsResponse = _client.Connections.List(_connectionData.AzureRMGroupName, _connectionData.AzureAutomationAccount);

            if (connectionsResponse.Connection.Count > 0)
            {
                foreach (var connection in connectionsResponse.Connection)
                {
                    var conn = new Vendor.Azure.Connection();
                    conn.Name = connection.Name;
                    conn.LastModifiedTime = connection.Properties.LastModifiedTime.DateTime;
                    conn.Description = connection.Properties.Description;
                    conn.ConnectionType = new Vendor.Azure.ConnectionType
                    {
                        Name = connection.Properties.ConnectionType.Name,
                        CreationTime = connection.Properties.CreationTime.DateTime,
                        LastModifiedTime = connection.Properties.LastModifiedTime.DateTime
                    };

                    foreach (var def in connection.Properties.FieldDefinitionValues)
                    {
                        conn.ConnectionFieldValues.Add(new Vendor.Azure.ConnectionFieldValue
                        {
                            Connection = conn,
                            ConnectionFieldName = def.Key,
                            ConnectionName = conn.Name,
                            ConnectionTypeName = conn.ConnectionType.Name,
                            IsEncrypted = false,
                            IsOptional = false,
                            Type = string.Empty,
                            Value = def.Value
                        });
                    }

                    _backendContext.AddToConnections(new ConnectionModelProxy(conn, _backendContext));
                }

                while (connectionsResponse.NextLink != null)
                {
                    connectionsResponse = _client.Connections.ListNext(connectionsResponse.NextLink);

                    foreach (var connection in connectionsResponse.Connection)
                    {
                        var conn = new Vendor.Azure.Connection();
                        conn.Name = connection.Name;
                        conn.LastModifiedTime = connection.Properties.LastModifiedTime.DateTime;
                        conn.Description = connection.Properties.Description;
                        conn.ConnectionType = new Vendor.Azure.ConnectionType
                        {
                            Name = connection.Properties.ConnectionType.Name,
                            CreationTime = connection.Properties.CreationTime.DateTime,
                            LastModifiedTime = connection.Properties.LastModifiedTime.DateTime
                        };

                        foreach (var def in connection.Properties.FieldDefinitionValues)
                        {
                            conn.ConnectionFieldValues.Add(new Vendor.Azure.ConnectionFieldValue
                            {
                                Connection = conn,
                                ConnectionFieldName = def.Key,
                                ConnectionName = conn.Name,
                                ConnectionTypeName = conn.ConnectionType.Name,
                                IsEncrypted = false,
                                IsOptional = false,
                                Type = string.Empty,
                                Value = def.Value
                            });
                        }

                        _backendContext.AddToConnections(new ConnectionModelProxy(conn, _backendContext));
                    }
                }
            }

            var modulesResponse = _client.Modules.List(_connectionData.AzureRMGroupName, _connectionData.AzureAutomationAccount);

            if (modulesResponse.Modules.Count > 0)
            {
                var modules = modulesResponse.Modules.Select(m => new Vendor.Azure.Module
                {
                    CreationTime = m.Properties.CreationTime.DateTime,
                    LastModifiedTime = m.Properties.LastModifiedTime.DateTime,
                    ModuleName = m.Name,
                    ModuleUrl = m.Location,
                    ModuleVersion = m.Properties.Version
                });

                foreach (var module in modules)
                    _backendContext.AddToModules(new ModuleModelProxy(module, _backendContext));

                while (modulesResponse.NextLink != null)
                {
                    modulesResponse = _client.Modules.ListNext(modulesResponse.NextLink);

                    modules = modulesResponse.Modules.Select(m => new Vendor.Azure.Module
                    {
                        CreationTime = m.Properties.CreationTime.DateTime,
                        LastModifiedTime = m.Properties.LastModifiedTime.DateTime,
                        ModuleName = m.Name,
                        ModuleUrl = m.Location,
                        ModuleVersion = m.Properties.Version
                    });

                    foreach (var module in modules)
                        _backendContext.AddToModules(new ModuleModelProxy(module, _backendContext));
                }
            }

            var schedulesResponse = _client.Schedules.List(_connectionData.AzureRMGroupName, _connectionData.AzureAutomationAccount);

            if (schedulesResponse.Schedules.Count > 0)
            {
                /*var schedules = schedulesResponse.Schedules.Select(s => new Vendor.Azure.Schedule
                {
                    Id = s.Id,
                    ScheduleID = Guid.NewGuid(),
                    DayInterval = (int)s.Properties.Interval,
                    ExpiryTime = s.Properties.ExpiryTime.DateTime,
                    
                });*/
                _backendContext.AddToSchedules(new ScheduleModelProxy(new Vendor.Azure.Schedule { Name = "Schedules not supported yet.", ScheduleID = Guid.NewGuid() }, _backendContext));
            }

            _backendContext.ParseTags();
            _backendContext.IsReady = true;

            Execute.OnUIThread(() =>
            {
                _backendContext.SignalCompleted();
            });
        }

        public async Task PauseExecution(RunbookModelProxy runbook, bool isDraft = false)
        {
            if (!_jobCache.ContainsKey(runbook.JobID) && !_testJobCache.ContainsKey(runbook.JobID))
                return;

            if (isDraft)
            {
                // Test job
                await _client.TestJobs.SuspendAsync(_connectionData.AzureRMGroupName, _connectionData.AzureAutomationAccount, runbook.RunbookName);
            }
            else
            {
                await _client.Jobs.SuspendAsync(_connectionData.AzureRMGroupName, _connectionData.AzureAutomationAccount, runbook.JobID);
            }
        }

        public async Task ResumeExecution(RunbookModelProxy runbook, bool isDraft = false)
        {
            if (!_jobCache.ContainsKey(runbook.JobID) && !_testJobCache.ContainsKey(runbook.JobID))
                return;

            if (isDraft)
            {
                // Test job
                await _client.TestJobs.ResumeAsync(_connectionData.AzureRMGroupName, _connectionData.AzureAutomationAccount, runbook.RunbookName);
            }
            else
            {
                await _client.Jobs.ResumeAsync(_connectionData.AzureRMGroupName, _connectionData.AzureAutomationAccount, runbook.JobID);
            }
        }

        public async Task<OperationResult> Save(IViewModel instance, Command command)
        {
            var operationResult = default(OperationResult);

            if (instance.Model is RunbookModelProxy)
            {
                operationResult = await SaveRunbookAsync(instance.Model as RunbookModelProxy, instance.Content);
            }
            else if (instance.Model is VariableModelProxy)
            {
                await SaveVariableAsync(instance.Model as VariableModelProxy);
            }
            else if (instance.Model is CredentialModelProxy)
            {
                await SaveAzureCredentialAsync(instance);
            }
            else if (instance.Model is ScheduleModelProxy)
            {
                SaveAzureSchedule(instance);
            }
            else if (instance.Model is ModuleModelProxy)
            {
                await SaveAzureModuleAsync(instance);
            }
            else if (instance.Model is ConnectionModelProxy)
            {
                await SaveAzureConnectionAsync(instance);
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

        public async Task<OperationResult> SaveRunbookAsync(RunbookModelProxy runbook, string runbookContent)
        {
            var runbookCreated = false;
            var response = default(RunbookCreateOrUpdateResponse);

            //if (runbook.RunbookID == Guid.Empty)
            //{
                //runbookNeedsCreation = true;

            // This is a new runbook!
            var draft = new RunbookDraft();
            draft.InEdit = true;
            draft.CreationTime = DateTimeOffset.Now;
            draft.LastModifiedTime = DateTimeOffset.Now;
                
            var details = new RunbookCreateOrUpdateDraftParameters();
            details.Name = runbook.RunbookName;
            details.Location = _connectionData.AzureRMLocation;
            details.Tags.Add(AutoStudioTagName, runbook.Tags != null ? runbook.Tags : string.Empty);

            details.Properties = new RunbookCreateOrUpdateDraftProperties();
            details.Properties.RunbookType = "Script";
            details.Properties.Description = "Runbook created with Automation Studio.";
            details.Properties.Draft = draft;

            response = await _client.Runbooks.CreateOrUpdateWithDraftAsync(_connectionData.AzureRMGroupName, _connectionData.AzureAutomationAccount, details);

            if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                runbookCreated = false;
            else
                runbookCreated = true;

            // Need to set the draft runbook ID to notify the system that this runbook is in draft mode
            runbook.DraftRunbookVersionID = Guid.NewGuid();
            //}

            if (!runbookCreated /*&& runbookNeedsCreation*/)
            {
                return new OperationResult
                {
                    ErrorCode = response.StatusCode.ToString(),
                    ErrorMessage = "Unable to save the runbook",
                    HttpStatusCode = response.StatusCode,
                    Status = OperationStatus.Failed
                };
            }

            // Now we need to commit the draft
            var status = await SaveRunbookContentAsync(runbook, runbookContent, RunbookType.Draft);

            // Make sure that we add the runbook to our Env Explorer
            if (runbook.RunbookID == Guid.Empty)
            {
                Context.Start();
            }

            return new OperationResult
            {
                HttpStatusCode = System.Net.HttpStatusCode.OK,
                Status = status,
                RequestUrl = string.Empty
            };
        }

        public async Task<OperationStatus> SaveRunbookContentAsync(RunbookModelProxy runbook, string runbookContent, RunbookType runbookType)
        {
            var longRunningOp = default(LongRunningOperationResultResponse);

            var runbookUpdate = new RunbookDraftUpdateParameters();
            runbookUpdate.Stream = runbookContent;
            runbookUpdate.Name = runbook.RunbookName;

            longRunningOp = await _client.RunbookDraft.UpdateAsync(_connectionData.AzureRMGroupName, _connectionData.AzureAutomationAccount, runbookUpdate);

            if (runbookType == RunbookType.Published)
            {
                await CheckIn(runbook);
            }

            if (longRunningOp.Status == Microsoft.Azure.OperationStatus.Failed)
                return OperationStatus.Failed;
            else if (longRunningOp.Status == Microsoft.Azure.OperationStatus.InProgress)
                return OperationStatus.InProgress;
            else if (longRunningOp.Status == Microsoft.Azure.OperationStatus.Succeeded)
                return OperationStatus.Succeeded;

            return OperationStatus.Failed;
        }

        public async Task<bool> SaveVariableAsync(VariableModelProxy variable)
        {
            var variableToSave = new VariableCreateOrUpdateParameters();
            variableToSave.Name = variable.Name;

            variableToSave.Properties = new VariableCreateOrUpdateProperties();
            variableToSave.Properties.IsEncrypted = variable.IsEncrypted;
            variableToSave.Properties.Value = variable.Value;

            var response = await _client.Variables.CreateOrUpdateAsync(_connectionData.AzureRMGroupName, _connectionData.AzureAutomationAccount, variableToSave);

            if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
            {
                _output.AppendLine("Unable to save the variable at the moment, please verify your connectivity and try again.");
                return false;
            }

            return true;
        }

        private async Task SaveAzureCredentialAsync(IViewModel viewModel)
        {
            var credential = viewModel.Model as CredentialModelProxy;

            var credToSave = new CredentialCreateOrUpdateParameters();
            credToSave.Name = credential.Name;

            credToSave.Properties = new CredentialCreateOrUpdateProperties();
            credToSave.Properties.UserName = credential.UserName;
            credToSave.Properties.Password = credential.RawValue;

            var response = await _client.PsCredentials.CreateOrUpdateAsync(_connectionData.AzureRMGroupName, _connectionData.AzureAutomationAccount, credToSave);

            if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
            {
                viewModel.UnsavedChanges = true;
                _output.AppendLine("Unable to save the credential at the moment, please verify your connectivity and try again.");
            }
            else
            {
                viewModel.UnsavedChanges = false;
            }

            credential.RawValue = string.Empty;
        }

        private void SaveAzureSchedule(IViewModel viewModel)
        {
            throw new NotImplementedException("Schedules not supported yet.");
        }

        private async Task SaveAzureModuleAsync(IViewModel viewModel)
        {
            var module = viewModel.Model as ModuleModelProxy;

            var contentLink = new ContentLink();
            contentLink.Uri = new Uri(module.ModuleUrl);
            contentLink.Version = module.ModuleVersion;

            var moduleToSave = new ModuleCreateOrUpdateParameters();
            moduleToSave.Name = module.ModuleName;
            moduleToSave.Location = _connectionData.AzureRMLocation;

            moduleToSave.Properties = new ModuleCreateOrUpdateProperties();
            moduleToSave.Properties.ContentLink = contentLink;

            var response = await _client.Modules.CreateOrUpdateAsync(_connectionData.AzureRMGroupName, _connectionData.AzureAutomationAccount, moduleToSave);

            if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
            {
                viewModel.UnsavedChanges = true;
                _output.AppendLine("Unable to save the module at the moment, please verify your connectivity and try again.");
            }
            else
            {
                viewModel.UnsavedChanges = false;
            }
        }

        private async Task SaveAzureConnectionAsync(IViewModel viewModel)
        {
            var connection = viewModel.Model as ConnectionModelProxy;

            var connectionToSave = new ConnectionCreateOrUpdateParameters();
            connectionToSave.Name = connection.Name;

            connectionToSave.Properties = new ConnectionCreateOrUpdateProperties();
            connectionToSave.Properties.ConnectionType = new ConnectionTypeAssociationProperty();

            var connectionType = (connection.ConnectionType as Vendor.Azure.ConnectionType);
            connectionToSave.Properties.ConnectionType.Name = connectionType.Name;

            connectionToSave.Properties.Description = connection.Description;

            var fieldValues = connection.ConnectionFieldValues as IList<Vendor.Azure.ConnectionFieldValue>;
            foreach (var key in fieldValues)
            {
                connectionToSave.Properties.FieldDefinitionValues.Add(key.ConnectionFieldName, key.Value);
            }

            var response = await _client.Connections.CreateOrUpdateAsync(_connectionData.AzureRMGroupName, _connectionData.AzureAutomationAccount, connectionToSave);

            if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
            {
                viewModel.UnsavedChanges = true;
                _output.AppendLine("Unable to save the connection at the moment, please verify your connectivity and try again.");
            }
            else
            {
                viewModel.UnsavedChanges = false;
            }
        }

        public Guid? StartRunbook(RunbookModelProxy runbookProxy, List<NameValuePair> parameters)
        {
            var jobGuid = Guid.NewGuid();
            
            var runbook = _client.Runbooks.Get(_connectionData.AzureRMGroupName, _connectionData.AzureAutomationAccount, runbookProxy.RunbookName);
            if (runbook.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            var jobParameters = new JobCreateParameters();
            jobParameters.Name = jobGuid.ToString();

            jobParameters.Properties = new JobCreateProperties();
            jobParameters.Properties.Runbook.Name = runbookProxy.RunbookName;

            foreach (var param in parameters)
                jobParameters.Properties.Parameters.Add(param.Name, param.Value);

            var response = _client.Jobs.Create(_connectionData.AzureRMGroupName, _connectionData.AzureAutomationAccount, jobParameters);
            
            if ((int)response.StatusCode > 299)
            {
                _output.AppendLine("Unable to start the job, please verify your connectivity and parameters and try again.");
                return null;
            }

            _jobCache.Add(jobGuid, response.Job);
            runbookProxy.IsTestRun = false;

            return jobGuid;
        }

        public async Task StopExecution(RunbookModelProxy runbook, bool isDraft = false)
        {
            if (!_jobCache.ContainsKey(runbook.JobID) && !_testJobCache.ContainsKey(runbook.JobID))
                return;
            
            if (isDraft)
            {
                // Test job
                await _client.TestJobs.StopAsync(_connectionData.AzureRMGroupName, _connectionData.AzureAutomationAccount, runbook.RunbookName);
            }
            else
            {
                await _client.Jobs.StopAsync(_connectionData.AzureRMGroupName, _connectionData.AzureAutomationAccount, runbook.JobID);
            }

            _jobCache.Remove(runbook.JobID);
        }

        public Guid? TestRunbook(RunbookModelProxy runbookProxy, List<NameValuePair> parameters)
        {
            var jobGuid = Guid.NewGuid();

            var runbook = _client.Runbooks.Get(_connectionData.AzureRMGroupName, _connectionData.AzureAutomationAccount, runbookProxy.RunbookName);
            if (runbook.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            var jobParameters = new TestJobCreateParameters();
            jobParameters.RunbookName = runbookProxy.RunbookName;

            foreach (var param in parameters)
                jobParameters.Parameters.Add(param.Name, param.Value);

            var response = _client.TestJobs.Create(_connectionData.AzureRMGroupName, _connectionData.AzureAutomationAccount, jobParameters);

            if ((int)response.StatusCode > 299)
            {
                _output.AppendLine("Unable to start the job, please verify your connectivity and parameters and try again.");
                return null;
            }

            _testJobCache.Add(jobGuid, response.TestJob);
            runbookProxy.IsTestRun = true;

            return jobGuid;
        }

        #region Authentication (Tokens)
        private async Task<AccessToken> GetTokenAsync(Guid tenantId)
        {
            var token = default(AccessToken);
            var refreshToken = default(AccessToken);

            if (_tokens.ContainsKey(tenantId))
            {
                token = _tokens[tenantId].FirstOrDefault(x => x.resource == AzureBaseUrl);
                refreshToken = _tokens[tenantId].FirstOrDefault();
            }

            if (refreshToken == null && _tokens.ContainsKey(default(Guid)))
                refreshToken = _tokens[default(Guid)].FirstOrDefault();

            if (token == null || token.ExpiresOn < DateTime.UtcNow.AddMinutes(-1))
                token = null;

            if (token == null && refreshToken != null && refreshToken.refresh_token != null)
            {
                try
                {
                    var request = new HttpRequestMessage();
                    request.Method = HttpMethod.Post;
                    
                    request.Headers.Add("Accept", "application/json");
                    request.Headers.Add("x-ms-version", "2016-02-01");

                    var restPacket = new RestPacket();
                    restPacket.Add("grant_type", "refresh_token");
                    restPacket.Add("refresh_token", refreshToken.refresh_token);

                    token = await AcquireTokenByRequestAsync(tenantId, restPacket, request);
                }
                catch
                {
                    token = null;
                }
            }

            if (token == null)
            {
                var request = new HttpRequestMessage();
                request.Method = HttpMethod.Post;
                
                request.Headers.Add("x-ms-version", "2016-02-01");

                // Add client secret information
                var restPacket = new RestPacket();
                restPacket.Add("client_id", _connectionData.AzureRMServicePrincipalId);
                restPacket.Add("redirect_uri", AzureRedirectUrl);
                restPacket.Add("grant_type", "client_credentials");
                restPacket.Add("client_secret", _connectionData.UnsecureDecrypt(_connectionData.AzureRMServicePrincipalKey));
                
                token = await AcquireTokenByRequestAsync(tenantId, restPacket, request);
            }

            if (!_tokens.ContainsKey(tenantId))
                _tokens.Add(tenantId, new List<AccessToken>());

            var existingToken = _tokens[tenantId].FirstOrDefault(x => x.resource == AzureBaseUrl);
            if (existingToken == null || existingToken.access_token != token.access_token)
            {
                if (existingToken != null)
                    _tokens[tenantId].Remove(existingToken);

                _tokens[tenantId].Add(existingToken);
            }

            return token;
        }

        private async Task<AccessToken> AcquireTokenByRequestAsync(Guid tenantId, RestPacket packet, HttpRequestMessage request)
        {
            packet.Add("resource", AzureBaseUrl);

            request.Headers.Add("Accept", "application/json;odata=verbose;charset=utf-8");
            request.Content = packet.GetFormData();

            var tenantStr = tenantId.ToString();
            if (tenantId.Equals(default(Guid)))
                tenantStr = "common";

            request.RequestUri = new Uri(string.Format(AzureTokenUrl, tenantStr));

            var httpClient = new HttpClient();
            var response = await httpClient.SendAsync(request);

            if ((int)response.StatusCode < 400)
            {
                var content = await response.Content.ReadAsStringAsync();
                var token = JsonConvert.DeserializeObject<AccessToken>(content);

                token.ExpiresOn = new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(token.expires_on);

                if (token != null)
                    return token;
            }

            var errorContent = await response.Content.ReadAsStringAsync();

            throw new ApplicationException($"Error {(int)response.StatusCode} - {response.StatusCode.ToString()}: {errorContent}");
        }
        #endregion
    }
}
