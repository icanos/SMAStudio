using Caliburn.Micro;
using Gemini.Framework;
using Gemini.Framework.Commands;
using Gemini.Framework.Services;
using Gemini.Modules.Output;
using SMAStudiovNext.Core;
using SMAStudiovNext.Icons;
using SMAStudiovNext.Language.Completion;
using SMAStudiovNext.Models;
using SMAStudiovNext.Modules.Runbook.ViewModels;
using SMAStudiovNext.SMA;
using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;

namespace SMAStudiovNext.Services
{
    public class SmaService : IBackendService
    {
        private readonly IList<string> _runningJobsDefinition = new List<string> { "Running", "Suspended", "New", "Activating" };
        private readonly IDictionary<Guid, bool> _runbookRunningCache;
        private readonly IDictionary<object, object> _viewModelsCache;
        private readonly IBackendContext _backendContext;
        private readonly BackendConnection _connectionData;

        public SmaService(IBackendContext context, BackendConnection connectionData)
        {
            _connectionData = connectionData;
            _backendContext = context;

            _viewModelsCache = new Dictionary<object, object>();
            _runbookRunningCache = new Dictionary<Guid, bool>();
            //_backendContext = AppContext.Resolve<IBackendContext>();
        }

        /// <summary>
        /// Loads all objects from SMA, such as Runbooks, Variables, Schedules and Credentials.
        /// This is then stored locally and an event is triggered to notify our Environment Explorer
        /// about the changes.
        /// </summary>
        public void Load()
        {
            Logger.DebugFormat("Load()");

            if (SettingsService.CurrentSettings == null)
                return;

            if (!String.IsNullOrEmpty(_connectionData.SmaConnectionUrl))
            {
                var context = GetConnection();

                if (context != null)
                {
                    //AsyncExecution.Run(System.Threading.ThreadPriority.Normal, delegate ()
                    Task.Run(() =>
                    {
                        var runbooks = context.Runbooks.OrderBy(r => r.RunbookName).ToList();
                        foreach (var runbook in runbooks)
                            _backendContext.AddToRunbooks(new RunbookModelProxy(runbook, Context));

                        var credentials = context.Credentials.OrderBy(c => c.Name).ToList();
                        foreach (var credential in credentials)
                            _backendContext.AddToCredentials(new CredentialModelProxy(credential, Context));

                        var variables = context.Variables.OrderBy(v => v.Name).ToList();
                        foreach (var variable in variables)
                            _backendContext.AddToVariables(new VariableModelProxy(variable, Context));

                        var schedules = context.Schedules.OrderBy(s => s.Name).ToList();
                        foreach (var schedule in schedules)
                            _backendContext.AddToSchedules(new ScheduleModelProxy(schedule, Context));
                    
                        _backendContext.ParseTags();

                        //AsyncExecution.ExecuteOnUIThread(delegate ()
                        Execute.OnUIThread(() =>
                        {
                            var output = IoC.Get<IOutput>();
                            output.AppendLine(" ");
                            output.AppendLine("Statistics:");
                            output.AppendLine("Found Runbooks: " + _backendContext.Runbooks.Count);
                            output.AppendLine("Found Credentials: " + _backendContext.Credentials.Count);
                            output.AppendLine("Found Variables: " + _backendContext.Variables.Count);
                            output.AppendLine("Found Schedules: " + _backendContext.Schedules.Count);
                            output.AppendLine(" ");

                            _backendContext.SignalCompleted();
                            _backendContext.IsReady = true;
                        });
                    });
                }
            }
        }

        public ResourceContainer GetStructure()
        {
            var resource = new ResourceContainer(_connectionData.Name, this, _backendContext.ContextType == ContextType.Azure ? IconsDescription.Cloud : IconsDescription.SMAStudio32);
            resource.Context = _backendContext;
            resource.IsExpanded = true;
            resource.Title = _connectionData.Name;

            // Runbooks
            var runbooks = new ResourceContainer("Runbooks", new Folder("Runbooks"), IconsDescription.Folder);
            runbooks.Context = _backendContext;
            runbooks.Items = _backendContext.Tags;
            runbooks.IsExpanded = true;
            resource.Items.Add(runbooks);

            // Credentials
            var credentials = new ResourceContainer("Credentials", new Folder("Credentials"), IconsDescription.Folder);
            credentials.Context = _backendContext;
            credentials.Items = _backendContext.Credentials;
            resource.Items.Add(credentials);

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

        public IList<ConnectionTypeModelProxy> GetConnectionTypes()
        {
            return null;
        }

        public ConnectionModelProxy GetConnectionDetails(ConnectionModelProxy connection)
        {
            return connection;
        }

        /// <summary>
        /// Save changes to a runbook, credential, schedule or variable.
        /// </summary>
        /// <param name="instance"></param>
        public async Task<OperationResult> Save(IViewModel instance, Command command)
        {
            Logger.DebugFormat("Save({0})", instance);
            var context = GetConnection();

            try {
                if (instance.Model is RunbookModelProxy)
                {
                    SaveSmaRunbook(context, instance);
                }
                else if (instance.Model is VariableModelProxy)
                {
                    var proxy = (VariableModelProxy)instance.Model;

                    if (proxy.GetSubType().Equals(typeof(SMA.Variable)))
                        SaveSmaVariable(context, instance);
                }
                else if (instance.Model is CredentialModelProxy)
                {
                    var proxy = (CredentialModelProxy)instance.Model;

                    if (proxy.GetSubType().Equals(typeof(SMA.Credential)))
                        SaveSmaCredential(context, instance);
                }
                else if (instance.Model is ScheduleModelProxy)
                {
                    var proxy = (ScheduleModelProxy)instance.Model;

                    if (proxy.GetSubType().Equals(typeof(SMA.Schedule)))
                        SaveSmaSchedule(context, instance);
                }
                else
                    throw new NotImplementedException();

                // And lastly, open the document (or put focus on it if its open)
                var shell = IoC.Get<IShell>();
                shell.OpenDocument((IDocument)instance);
            }
            catch (DataServiceQueryException ex)
            {
                var xml = default(string);

                if (ex.InnerException != null)
                    xml = ex.InnerException.Message;
                else
                    xml = ex.Message;

                Logger.Error("Error when saving the object.", ex);
                XmlExceptionHandler.Show(xml);
            }

            if (command != null)
                Execute.OnUIThread(() => { command.Enabled = true; });

            return new OperationResult
            {
                Status = OperationStatus.Succeeded,
                HttpStatusCode = HttpStatusCode.OK
            };
        }

        private void SaveSmaSchedule(OrchestratorApi context, IViewModel instance)
        {
            Logger.DebugFormat("SaveSmaSchedule(...)");

            var schedule = (SMA.Schedule)((ScheduleModelProxy)instance.Model).Model;

            if (schedule.ScheduleID == Guid.Empty)
            {
                context.AddToSchedules(schedule);
            }
            else
            {
                var foundSchedule = context.Schedules.Where(s => s.ScheduleID == schedule.ScheduleID).FirstOrDefault();

                if (foundSchedule == null)
                {
                    // The variable doesn't exist
                    // NOTE: This suggests that the schedule may be created in another
                    // environment and then reconnected to another SMA instance. How should this be handled?
                    context.AddToSchedules(schedule);
                }

                foundSchedule.Name = schedule.Name;

                context.UpdateObject(foundSchedule);
            }

            context.SaveChanges();
        }

        private void SaveSmaCredential(OrchestratorApi context, IViewModel instance)
        {
            Logger.DebugFormat("SaveSmaCredential(...)");

            var credential = (SMA.Credential)((CredentialModelProxy)instance.Model).Model;

            if (credential.CredentialID == Guid.Empty)
            {
                context.AddToCredentials(credential);
            }
            else
            {
                var foundCredential = context.Credentials.Where(c => c.CredentialID == credential.CredentialID).FirstOrDefault();

                if (foundCredential == null)
                {
                    // The variable doesn't exist
                    // NOTE: This suggests that the credential may be created in another
                    // environment and then reconnected to another SMA instance. How should this be handled?
                    context.AddToCredentials(credential);
                }

                foundCredential.Name = credential.Name;
                foundCredential.UserName = credential.UserName;
                foundCredential.RawValue = credential.RawValue;

                context.UpdateObject(foundCredential);
            }

            context.SaveChanges();
        }
        
        private void SaveSmaVariable(OrchestratorApi context, IViewModel instance)
        {
            Logger.DebugFormat("SaveSmaVariable(...)");

            var variable = (SMA.Variable)((VariableModelProxy)instance.Model).Model;

            if (variable.VariableID == Guid.Empty)
            {
                context.AddToVariables(variable);
            }
            else
            {
                var foundVariable = context.Variables.Where(v => v.VariableID == variable.VariableID).FirstOrDefault();

                if (foundVariable == null)
                {
                    // The variable doesn't exist
                    // NOTE: This suggests that the variable may be created in another
                    // environment and then reconnected to another SMA instance. How should this be handled?
                    context.AddToVariables(variable);
                }

                foundVariable.Name = variable.Name;

                if (!foundVariable.IsEncrypted)
                {
                    foundVariable.Value = variable.Value;
                    foundVariable.IsEncrypted = variable.IsEncrypted;
                }

                context.UpdateObject(foundVariable);
            }

            context.SaveChanges();
        }

        private void SaveSmaRunbook(OrchestratorApi context, IViewModel instance)
        {
            Logger.DebugFormat("SaveSmaRunbook(...)");

            var runbook = (SMA.Runbook)((RunbookModelProxy)instance.Model).Model;

            if (runbook == null || runbook.RunbookID == Guid.Empty)
            {
                Logger.DebugFormat("Runbook does not exist yet, generate a new ID and set it as draft.");

                // This is a new runbook
                var runbookVersion = new RunbookVersion
                {
                    TenantID = new Guid("00000000-0000-0000-0000-000000000000"),
                    IsDraft = true
                };

                context.AddToRunbookVersions(runbookVersion);

                var ms = new MemoryStream();
                var bytes = Encoding.UTF8.GetBytes(instance.Content);
                ms.Write(bytes, 0, bytes.Length);
                ms.Seek(0, SeekOrigin.Begin);

                var baseStream = (Stream)ms;

                context.SetSaveStream(runbookVersion, baseStream, true, "application/octet-stream", string.Empty);

                EntityDescriptor ed = null;
                try
                {
                    ChangeOperationResponse cor =
                        (ChangeOperationResponse)context.SaveChanges().FirstOrDefault<OperationResponse>();

                    if (cor != null)
                    {
                        ed = (cor.Descriptor as EntityDescriptor);
                    }
                }
                catch (Exception e)
                {
                    Logger.Error("Unable to verify the saved runbook.", e);
                    throw new PersistenceException("Sorry, we were unable to save your runbook. Please refer to the log for more information.");
                }

                if (ed != null && ed.EditLink != null)
                {
                    MergeOption mergeOption = context.MergeOption;
                    context.MergeOption = MergeOption.OverwriteChanges;
                    try
                    {
                        context.Execute<RunbookVersion>(ed.EditLink).Count<RunbookVersion>();
                    }
                    catch (Exception e)
                    {
                        Logger.Error("Unable to save the runbook.", e);
                        throw new PersistenceException("There was an error when saving the runbook. Please try again later.");
                    }
                    finally
                    {
                        context.MergeOption = mergeOption;
                    }
                }

                var savedRunbook = context.Runbooks.Where(x => x.RunbookID == runbookVersion.RunbookID).FirstOrDefault();

                if (savedRunbook == null)
                {
                    throw new PersistenceException("Unable to retrieve the saved runbook, something went wrong when trying to save the object. Please try again.");
                }

                instance.Model = savedRunbook;
                runbook = savedRunbook;
            }

            try
            {
                context.AttachTo("Runbooks", runbook);
            }
            catch (InvalidOperationException) { /* already attached */ }

            // Save the updated runbook
            if (!runbook.DraftRunbookVersionID.HasValue || runbook.DraftRunbookVersionID == Guid.Empty)
            {
                runbook.DraftRunbookVersionID = new Guid?(runbook.Edit(context));
            }

            try
            {
                var ms = new MemoryStream();
                var bytes = Encoding.UTF8.GetBytes(instance.Content);
                ms.Write(bytes, 0, bytes.Length);
                ms.Seek(0, SeekOrigin.Begin);

                var baseStream = (Stream)ms;
                var entity = (from rv in context.RunbookVersions
                              where (Guid?)rv.RunbookVersionID == runbook.DraftRunbookVersionID
                              select rv).FirstOrDefault<RunbookVersion>();

                try
                {
                    context.AttachTo("Runbooks", runbook);
                }
                catch (InvalidOperationException) { }

                context.SetSaveStream(entity, baseStream, true, "application/octet-stream", string.Empty);
                context.SaveChanges();

                var smaRunbook = context.Runbooks.Where(r => r.RunbookID.Equals(runbook.RunbookID)).FirstOrDefault();
                smaRunbook.Tags = runbook.Tags;
                smaRunbook.Description = runbook.Description;
                smaRunbook.DraftRunbookVersionID = runbook.DraftRunbookVersionID;
                smaRunbook.PublishedRunbookVersionID = runbook.PublishedRunbookVersionID;

                context.UpdateObject(smaRunbook);
                context.SaveChanges();

                instance.UnsavedChanges = false;
            }
            catch (Exception e)
            {
                Logger.Error("Error when saving the runbook.", e);
                throw new PersistenceException("Unable to save the changes, error: " + e.Message);
            }
        }

        public bool Delete(ModelProxyBase model)
        {
            Logger.DebugFormat("Delete(...)");

            if (model is RunbookModelProxy)
                return DeleteRunbook((Runbook)model.Model);
            else if (model is VariableModelProxy)
                return DeleteVariable((Variable)model.Model);
            else if (model is CredentialModelProxy)
                return DeleteCredential((Credential)model.Model);
            else if (model is ScheduleModelProxy)
                return DeleteSchedule((Schedule)model.Model);

            return false;
        }

        private bool DeleteRunbook(Runbook runbook)
        {
            Logger.DebugFormat("DeleteRunbook(...)");

            var context = GetConnection();
            try
            {
                var foundRunbook = context.Runbooks.Where(r => r.RunbookID == runbook.RunbookID).FirstOrDefault();

                if (foundRunbook == null)
                    return false;

                context.DeleteObject(foundRunbook);
                context.SaveChanges();
            }
            catch (DataServiceClientException ex)
            {
                Logger.Error("Error when deleting the runbook.", ex);
                return false;
            }

            return true;
        }

        private bool DeleteVariable(Variable variable)
        {
            Logger.DebugFormat("DeleteVariable(...)");

            var context = GetConnection();
            try
            {
                var foundVariable = context.Variables.Where(v => v.VariableID == variable.VariableID).FirstOrDefault();

                if (foundVariable == null)
                    return false;

                context.DeleteObject(foundVariable);
                context.SaveChanges();
            }
            catch (DataServiceQueryException ex)
            {
                Logger.Error("Error when deleting the variable.", ex);
                return false; // Probably already deleted
            }

            return true;
        }

        private bool DeleteCredential(Credential credential)
        {
            Logger.DebugFormat("DeleteCredential(...)");

            var context = GetConnection();
            try
            {
                var foundCredential = context.Credentials.Where(c => c.CredentialID == credential.CredentialID).FirstOrDefault();

                if (foundCredential == null)
                    return false;

                context.DeleteObject(foundCredential);
                context.SaveChanges();
            }
            catch (DataServiceQueryException ex)
            {
                Logger.Error("Error when deleting the credential.", ex);
                return false; // Probably already deleted
            }

            return true;
        }

        private bool DeleteSchedule(Schedule schedule)
        {
            Logger.DebugFormat("DeleteSchedule(...)");

            var context = GetConnection();
            try
            {
                var foundSchedule = context.Schedules.Where(s => s.ScheduleID == schedule.ScheduleID).FirstOrDefault();

                if (foundSchedule == null)
                    return false;

                context.DeleteObject(foundSchedule);
                context.SaveChanges();
            }
            catch (DataServiceQueryException ex)
            {
                Logger.Error("Error when deleting the schedule.", ex);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Retrieve a connection to SMA based on impersonated credentials
        /// or credentials provided when configured the service.
        /// </summary>
        /// <returns></returns>
        public OrchestratorApi GetConnection(bool silent = false)
        {
            Logger.DebugFormat("GetConnection(silent = {0})", silent ? "True" : "False");

            var connection = new OrchestratorApi(new Uri(_connectionData.SmaConnectionUrl));

            if (_connectionData.SmaImpersonatedLogin)
                ((DataServiceContext)connection).Credentials = CredentialCache.DefaultCredentials;
            else
                ((DataServiceContext)connection).Credentials = new NetworkCredential(_connectionData.SmaUsername, _connectionData.GetPassword(), _connectionData.SmaDomain);

            // Retrieve a single runbook in order to display the message box if cert is invalid (then load everything async)
            try
            {
                connection.Runbooks.First();
            }
            catch (DataServiceTransportException ex)
            {
                Logger.Error("Unable to establish a connection to SMA.", ex);

                if (!silent)
                    MessageBox.Show("A connection could not be established to the Service Management Automation webservice, please verify connectivity. Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                return null;
            }

            return connection;
        }

        /// <summary>
        /// Retrieve information about a specific job from SMA
        /// </summary>
        /// <param name="jobId">ID to retrieve information about</param>
        /// <returns>Proxy object or null</returns>
        public JobModelProxy GetJobDetails(RunbookModelProxy runbook)
        {
            Logger.DebugFormat("GetJobDetails(runbook = {0})", runbook.RunbookName);

            return GetJobDetails(runbook.JobID);
        }

        private DateTime lastJobDownloadTime = DateTime.MinValue;
        public JobModelProxy GetJobDetails(Guid jobId)
        {
            Logger.DebugFormat("GetJobDetails(jobId = {0})", jobId);

            var context = GetConnection();
            var model = context.Jobs.Where(j => j.JobID.Equals(jobId)).Select(j => new JobModelProxy(j, Context)).FirstOrDefault();

            //if (model.LastDownloadTime != null)
            //{
            var entries = GetJobContent(jobId, "Any");
            model.Result = entries.Where(e => e.StreamTime > lastJobDownloadTime).ToList();
            /*}
            else
                model.Result = GetJobContent(jobId, "Any");*/

            var entry = model.Result.OrderByDescending(m => m.StreamTime).FirstOrDefault();

            if (entry != null)
                lastJobDownloadTime = entry.StreamTime;

            return model;
        }

        /// <summary>
        /// Download job information from SMA
        /// </summary>
        /// <param name="jobId">ID to download content from</param>
        /// <returns>List of output</returns>
        private IList<JobOutput> GetJobContent(Guid jobId, string streamType = "Any")
        {
            Logger.DebugFormat("GetJobContent(jobId = {0}, streamType = {1})", jobId, streamType);

            var request = (HttpWebRequest)WebRequest.Create(_connectionData.SmaConnectionUrl + "/JobStreams/GetStreamItems?jobId='" + jobId + "'&streamType='" + streamType + "'");
            if (_connectionData.SmaImpersonatedLogin)
                request.Credentials = CredentialCache.DefaultCredentials;
            else
                request.Credentials = new NetworkCredential(_connectionData.SmaUsername, _connectionData.GetPassword(), _connectionData.SmaDomain);

            var response = default(HttpWebResponse);

            try
            {
                response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException e)
            {
                Logger.Error("Error when communicating with the SMA webservice.", e);

                var output = IoC.Get<IOutput>();
                output.AppendLine("Unable to load data about job '" + jobId + "' from SMA. Error: " + e.Message);

                MessageBox.Show("Error when retrieving job. Please refer to the output window.", "Error", MessageBoxButton.OK);
                return null;
            }

            // Retrieve the content
            var tr = new StreamReader(response.GetResponseStream());
            var content = tr.ReadToEnd();

            tr.Close();

            // Sanitize the retrieved information (remove NUL chars)
            content = content.Replace(Convert.ToChar(0x0).ToString(), "");
            content = content.Replace("&#x0;", "");

            var outputXml = XElement.Parse(content);
            XNamespace d = "http://schemas.microsoft.com/ado/2007/08/dataservices";
            XNamespace m = "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata";
            XNamespace a = "http://www.w3.org/2005/Atom";
            var entries = outputXml.Elements(a + "entry");//.Elements(m + "properties");

            var outputItems = new List<JobOutput>();
            foreach (var entry in entries)
            {
                var propertyContainers = entry.Element(a + "content").Element(m + "properties").Elements();
                var outputItem = new JobOutput();

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

            return outputItems;
        }

        /// <summary>
        /// Check in a drafted runbook and make it the published one.
        /// </summary>
        /// <param name="runbook"></param>
        /// <returns></returns>
        public async Task<bool> CheckIn(RunbookModelProxy runbook)
        {
            Logger.DebugFormat("CheckIn(runbook = {0})", runbook.RunbookName);

            return await Task.Run(delegate ()
            {
                var context = GetConnection();

                try
                {
                    context.AttachTo("Runbooks", runbook.Model);
                }
                catch (InvalidOperationException) { /* already attached */ }

                if (!runbook.DraftRunbookVersionID.HasValue || runbook.DraftRunbookVersionID == Guid.Empty)
                {
                    MessageBox.Show("The runbook's already checked in.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    return true;
                }

                var publishedGuid = Guid.Empty;

                try
                {
                    //runbook.PublishedRunbookVersionID = ((Runbook)runbook.Model).Publish(context);
                    publishedGuid = ((Runbook)runbook.Model).Publish(context);
                }
                catch (DataServiceQueryException ex)
                {
                    var xml = default(string);

                    if (ex.InnerException != null)
                        xml = ex.InnerException.Message;
                    else
                        xml = ex.Message;

                    Logger.Error("Error when publishing the runbook.", ex);
                    XmlExceptionHandler.Show(xml);

                    return false;
                }

                runbook.PublishedRunbookVersionID = publishedGuid;

                return true;
            });
        }

        /// <summary>
        /// Check out the published runbook and make it a draft, this will overwrite any drafts currently there.
        /// </summary>
        /// <param name="runbookViewModel"></param>
        /// <returns></returns>
        public async Task<bool> CheckOut(RunbookViewModel runbookViewModel)
        {
            Logger.DebugFormat("CheckOut(runbook = {0})", runbookViewModel.DisplayName);

            return await Task.Run(delegate ()
            {
                var context = GetConnection();
                var runbook = runbookViewModel.Runbook;

                try
                {
                    context.AttachTo("Runbooks", runbook.Model);
                }
                catch (InvalidOperationException) { /* already attached */ }

                if (!runbook.DraftRunbookVersionID.HasValue || runbook.DraftRunbookVersionID == Guid.Empty)
                {
                    try
                    {
                        runbook.DraftRunbookVersionID = new Guid?(((SMA.Runbook)runbook.Model).Edit(context));
                    }
                    catch (DataServiceQueryException ex)
                    {
                        var xml = default(string);

                        if (ex.InnerException != null)
                            xml = ex.InnerException.Message;
                        else
                            xml = ex.Message;

                        Logger.Error("Error when checking out the runbook.", ex);
                        XmlExceptionHandler.Show(xml);

                        return false;
                    }
                }
                else
                {
                    //Core.Log.ErrorFormat("The runbook was already checked out.");
                    MessageBox.Show("The runbook's already checked out.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    return false;
                }

                // Download the content of the published runbook
                var request = (HttpWebRequest)WebRequest.Create(_connectionData.SmaConnectionUrl + "/Runbooks(guid'" + runbook.RunbookID + "')/PublishedRunbookVersion/$value");
                if (_connectionData.SmaImpersonatedLogin)
                    request.Credentials = CredentialCache.DefaultCredentials;
                else
                    request.Credentials = new NetworkCredential(_connectionData.SmaUsername, _connectionData.GetPassword(), _connectionData.SmaDomain);

                var response = (HttpWebResponse)request.GetResponse();
                var reader = new StreamReader(response.GetResponseStream());

                string content = reader.ReadToEnd();

                reader.Close();

                runbookViewModel.Content = content;

                MemoryStream ms = new MemoryStream();
                byte[] bytes = Encoding.UTF8.GetBytes(runbookViewModel.Content);
                ms.Write(bytes, 0, bytes.Length);
                ms.Seek(0, SeekOrigin.Begin);

                Stream baseStream = (Stream)ms;
                RunbookVersion entity = (from rv in context.RunbookVersions
                                         where (Guid?)rv.RunbookVersionID == runbook.DraftRunbookVersionID
                                         select rv).FirstOrDefault<RunbookVersion>();

                context.SetSaveStream(entity, baseStream, true, "application/octet-stream", string.Empty);
                context.SaveChanges();
                
                return true;
            });
        }

        /// <summary>
        /// Checks if the provided runbook has any running/suspended/new or activating jobs.
        /// </summary>
        /// <param name="runbook">Runbook to check</param>
        /// <param name="checkDraft">If we should check the draft or published version</param>
        /// <returns>True/false</returns>
        public async Task<bool> CheckRunningJobs(RunbookModelProxy runbook, bool checkDraft)
        {
            Logger.DebugFormat("CheckRunningJobs(runbook = {0}, checkDraft = {1})", runbook.RunbookName, checkDraft ? "True" : "False");
            var context = GetConnection();

            return await Task.Run(delegate ()
                {
                    var runbookVersionId = checkDraft ? runbook.DraftRunbookVersionID : runbook.PublishedRunbookVersionID;
                    var jobContexts = context.JobContexts.Where(x => x.RunbookVersionID.Equals(runbookVersionId)).ToList();

                    var runningJobDefinitionsStr = String.Join(",", _runningJobsDefinition);
                    var job = context.Jobs.Where(x => runningJobDefinitionsStr.Contains(x.JobStatus)).OrderByDescending(x => x.StartTime).FirstOrDefault();

                    if (job == null)
                        return false;

                    var matching = jobContexts.FirstOrDefault(x => x.JobContextID.Equals(job.JobContextID));

                    if (matching == null)
                        return false;

                    return true;
                });
        }

        public Guid? TestRunbook(RunbookModelProxy runbookProxy, List<NameValuePair> parameters)
        {
            Logger.DebugFormat("TestRunbook(...)");

            if (!(runbookProxy.Model is SMA.Runbook))
                return null;

            var context = GetConnection();
            var runbook = (SMA.Runbook)runbookProxy.Model;

            try
            {
                runbookProxy.IsTestRun = true;
                return runbook.TestRunbook(context, parameters);
            }
            catch (DataServiceQueryException ex)
            {
                var xml = default(string);

                if (ex.InnerException != null)
                    xml = ex.InnerException.Message;
                else
                    xml = ex.Message;

                Logger.Error("Error when testing the runbook.", ex);
                XmlExceptionHandler.Show(xml);
            }

            return null;
        }

        public IList<JobModelProxy> GetJobs(Guid runbookVersionId)
        {
            Logger.DebugFormat("GetJobs(runbookVersionId = {0})", runbookVersionId);

            var context = GetConnection();

            var jobContexts = context.JobContexts.Where(j => j.RunbookVersionID == runbookVersionId).ToList();
            var jobs = new List<JobModelProxy>();

            foreach (var jobContext in jobContexts)
            {
                var job = context.Jobs.Where(j => j.JobContextID == jobContext.JobContextID).FirstOrDefault();

                if (job == null)
                    continue;

                jobs.Add(new JobModelProxy(job, Context));
            }
            //var jobs = context.Jobs.Where(j => jobContexts.Where(jc => jc.JobContextID == j.JobContextID).FirstOrDefault() != null).ToList();

            return jobs;
        }

        /// <summary>
        /// Pauses execution of a runbook in SMA
        /// </summary>
        /// <param name="jobId">ID of the job to pause</param>
        public void PauseExecution(Guid jobId)
        {
            Logger.DebugFormat("PauseExecution(jobId = {0})", jobId);

            var context = GetConnection();
            var job = context.Jobs.Where(j => j.JobID == jobId).FirstOrDefault();

            if (job == null)
                return;

            try
            {
                job.Suspend(context);
            }
            catch (DataServiceQueryException ex)
            {
                var xml = default(string);

                if (ex.InnerException != null)
                    xml = ex.InnerException.Message;
                else
                    xml = ex.Message;

                Logger.Error("Error when trying to pause the runbook.", ex);
                XmlExceptionHandler.Show(xml);
            }
        }

        /// <summary>
        /// Resumes execution of a runbook
        /// </summary>
        /// <param name="jobId">ID of the job to resume</param>
        public void ResumeExecution(Guid jobId)
        {
            Logger.DebugFormat("ResumeExecution(jobId = {0})", jobId);

            var context = GetConnection();
            var job = context.Jobs.Where(j => j.JobID == jobId).FirstOrDefault();

            if (job == null)
                return;

            try
            {
                job.Resume(context);
            }
            catch (DataServiceQueryException ex)
            {
                var xml = default(string);

                if (ex.InnerException != null)
                    xml = ex.InnerException.Message;
                else
                    xml = ex.Message;

                Logger.Error("Error when trying to resume the runbook.", ex);
                XmlExceptionHandler.Show(xml);
            }
        }

        /// <summary>
        /// Stops execution of a runbook
        /// </summary>
        /// <param name="jobId">ID of the job to stop</param>
        public void StopExecution(Guid jobId)
        {
            Logger.DebugFormat("StopExecution(jobId = {0})", jobId);

            var context = GetConnection();
            var job = context.Jobs.Where(j => j.JobID == jobId).FirstOrDefault();

            if (job == null)
                return;

            try
            {
                job.Stop(context);
            }
            catch (DataServiceQueryException ex)
            {
                var xml = default(string);

                if (ex.InnerException != null)
                    xml = ex.InnerException.Message;
                else
                    xml = ex.Message;

                Logger.Error("Error when trying to stop the runbook.", ex);
                XmlExceptionHandler.Show(xml);
            }
        }

        public Guid? StartRunbook(RunbookModelProxy runbookProxy, List<NameValuePair> parameters)
        {
            Logger.DebugFormat("StartRunbook(runbook = {0}, ...)", runbookProxy.RunbookName);

            if (!(runbookProxy.Model is SMA.Runbook))
                return null;

            var context = GetConnection();
            var runbook = (SMA.Runbook)runbookProxy.Model;

            try
            {
                runbookProxy.IsTestRun = false;
                return runbook.StartRunbook(context, parameters);
            }
            catch (DataServiceQueryException ex)
            {
                var xml = default(string);

                if (ex.InnerException != null)
                    xml = ex.InnerException.Message;
                else
                    xml = ex.Message;

                Logger.Error("Error when trying to start the runbook.", ex);
                XmlExceptionHandler.Show(xml);
            }

            return null;
        }

        public string GetContent(string url)
        {
            Logger.DebugFormat("GetContent(url = {0})", url);

            var content = string.Empty;

            var request = (HttpWebRequest)HttpWebRequest.Create(url);
            if (_connectionData.SmaImpersonatedLogin)
                request.Credentials = CredentialCache.DefaultCredentials;
            else
                request.Credentials = new NetworkCredential(_connectionData.SmaUsername, _connectionData.GetPassword(), _connectionData.SmaDomain);

            var response = (HttpWebResponse)request.GetResponse();
            var reader = (TextReader)new StreamReader(response.GetResponseStream());

            content = reader.ReadToEnd();

            reader.Close();

            return content;
        }

        public string GetBackendUrl(RunbookType runbookType, RunbookModelProxy runbook)
        {
            Logger.DebugFormat("GetBackendUrl(runbookType = {0}, runbook = {1})", runbookType, runbook.RunbookName);

            switch (runbookType)
            {
                case RunbookType.Draft:
                    return _connectionData.SmaConnectionUrl + "/Runbooks(guid'" + runbook.RunbookID + "')/DraftRunbookVersion/$value";
                case RunbookType.Published:
                    return _connectionData.SmaConnectionUrl + "/Runbooks(guid'" + runbook.RunbookID + "')/PublishedRunbookVersion/$value";
            }

            return string.Empty;
        }

        public IList<ICompletionEntry> GetParameters(RunbookViewModel runbookViewModel, KeywordCompletionData completionData)
        {
            return completionData.Parameters;
        }

        public IBackendContext Context
        {
            get { return _backendContext; }
        }
    }
}
