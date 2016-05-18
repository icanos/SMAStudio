using Gemini.Framework.Commands;
using Microsoft.Azure;
using Microsoft.Azure.Management.Automation;
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
using System.Threading.Tasks;

namespace SMAStudiovNext.Services
{
    public class AzureRMService : IBackendService
    {
        private const string AzureBaseUrl = "https://management.core.windows.net/";
        private const string AzureResourceUrl = "subscriptions/{0}/resourceGroups/{1}/providers/Microsoft.Automation/automationAccounts/{2}/{3}?api-version=2015-10-31";
        private const string AzureXNS = "http://schemas.microsoft.com/windowsazure";
        private const string AzureTokenUrl = "https://login.microsoftonline.com/{0}/oauth2/token?api-version=1.0";
        private const string AzureRedirectUrl = "urn:ietf:wg:oauth:2.0:oob";

        private readonly IBackendContext _backendContext;
        private readonly BackendConnection _connectionData;
        private readonly IDictionary<Guid, List<AccessToken>> _tokens;
        private readonly WebRequestHandler _webRequestHandler;

        private TokenCloudCredentials _accessToken;
        private AutomationManagementClient _client;
        private X509Certificate2 _certificate = null;

        public IBackendContext Context
        {
            get { return _backendContext; }
        }

        public AzureRMService(IBackendContext context, BackendConnection connectionData)
        {
            _backendContext = context;
            _connectionData = connectionData;
            _tokens = new Dictionary<Guid, List<AccessToken>>();

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
        }

        private async Task RefreshTokenAsync()
        {
            var token = await GetTokenAsync(Guid.Parse(_connectionData.AzureRMTenantId));
            
            if (token == null)
                throw new InvalidOperationException("Sorry, a token could not be accquired. Please verify your Service Principal.");
            
            if (_accessToken == null)
                _accessToken = new TokenCloudCredentials(_connectionData.AzureSubscriptionId, token.access_token);
        }

        public Task<bool> CheckIn(RunbookModelProxy runbook)
        {
            throw new NotImplementedException();
        }

        public Task<bool> CheckOut(RunbookViewModel runbook)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> CheckRunningJobs(RunbookModelProxy runbook, bool checkDraft)
        {
            return await Task.Run(() => { return false; });
        }

        public bool Delete(ModelProxyBase model)
        {
            throw new NotImplementedException();
        }

        public string GetBackendUrl(RunbookType runbookType, RunbookModelProxy runbook)
        {
            // Azure RM, apart from Classic, only requires the runbook name when fetching the content.
            // That's why we only return runbook name here.
            return runbookType + "|" + runbook.RunbookName;
        }

        public ConnectionModelProxy GetConnectionDetails(ConnectionModelProxy connection)
        {
            throw new NotImplementedException();
        }

        public IList<ConnectionTypeModelProxy> GetConnectionTypes()
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public JobModelProxy GetJobDetails(RunbookModelProxy runbooks)
        {
            throw new NotImplementedException();
        }

        public Task<JobModelProxy> GetJobInformationAsync(Guid jobId)
        {
            throw new NotImplementedException();
        }

        public IList<JobModelProxy> GetJobs(Guid runbookVersionId)
        {
            throw new NotImplementedException();
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
            //_client.Runbooks.List(_connectionData.AzureAutomationAccount);

            if (runbooksResponse.Runbooks.Count > 0)
            {
                var runbooks = runbooksResponse.Runbooks.Select(r => new Vendor.Azure.Runbook
                {
                    Id = r.Id,
                    RunbookID = Guid.NewGuid(),
                    RunbookName = r.Name,
                    State = r.Properties.State
                }).ToList();

                foreach (var runbook in runbooks)
                    _backendContext.Runbooks.Add(new ResourceContainer(runbook.RunbookName, new RunbookModelProxy(runbook, _backendContext), IconsDescription.Runbook));
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
                    _backendContext.Variables.Add(new ResourceContainer(variable.Name, new VariableModelProxy(variable, _backendContext), IconsDescription.Variable));
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
                    _backendContext.Credentials.Add(new ResourceContainer(credential.Name, new CredentialModelProxy(credential, _backendContext), IconsDescription.Credential));
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

                    _backendContext.Connections.Add(new ResourceContainer(conn.Name, new ConnectionModelProxy(conn, _backendContext), IconsDescription.Connection));
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
                    _backendContext.Modules.Add(new ResourceContainer(module.ModuleName, new ModuleModelProxy(module, _backendContext), IconsDescription.Folder));
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
                _backendContext.Schedules.Add(new ResourceContainer("Schedules not supported yet.", new ScheduleModelProxy(new Vendor.Azure.Schedule(), _backendContext), IconsDescription.Schedule));
            }

            _backendContext.ParseTags();
            _backendContext.IsReady = true;
        }

        public Task PauseExecution(RunbookModelProxy runbook, bool isDraft = false)
        {
            throw new NotImplementedException();
        }

        public Task ResumeExecution(RunbookModelProxy runbook, bool isDraft = false)
        {
            throw new NotImplementedException();
        }

        public Task<OperationResult> Save(IViewModel instance, Command command)
        {
            throw new NotImplementedException();
        }

        public Guid? StartRunbook(RunbookModelProxy runbookProxy, List<NameValuePair> parameters)
        {
            throw new NotImplementedException();
        }

        public Task StopExecution(RunbookModelProxy runbook, bool isDraft = false)
        {
            throw new NotImplementedException();
        }

        public Guid? TestRunbook(RunbookModelProxy runbookProxy, List<NameValuePair> parameters)
        {
            throw new NotImplementedException();
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
