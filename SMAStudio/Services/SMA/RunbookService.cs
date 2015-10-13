using SMAStudio.Settings;
using SMAStudio.SMAWebService;
using SMAStudio.Util;
using SMAStudio.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Services.Client;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;

namespace SMAStudio.Services
{
    /// <summary>
    /// Service responsible for retrieving runbooks from SMA
    /// </summary>
    public class RunbookService : BaseService, IRunbookService
    {
        private IApiService _api;
        private IList<Runbook> _runbookCache = null;
        private IList<string> _tagCache = null;
        private ObservableCollection<RunbookViewModel> _runbookViewModelCache = null;
        private ObservableCollection<TagViewModel> _tagViewModelCache = null;

        private IWorkspaceViewModel _workspaceViewModel;
        private IComponentsViewModel _componentsViewModel;

        private DateTime _lastCheckedSuspendedJobs = DateTime.MinValue;
        private Guid _lastSuspendedJobID = Guid.Empty;

        private DateTime _lastCheckedRunningJobs = DateTime.MinValue;
        private Guid _lastRunningJobID = Guid.Empty;

        public RunbookService()
        {
            _api = Core.Resolve<IApiService>();
            _workspaceViewModel = Core.Resolve<IWorkspaceViewModel>();
            _componentsViewModel = Core.Resolve<IComponentsViewModel>();
        }

        public IList<Runbook> GetRunbooks(bool forceDownload = false)
        {
            try
            {
                if (_runbookCache == null || forceDownload)
                    _runbookCache = _api.Current.Runbooks.OrderBy(r => r.RunbookName).ToList();

                return _runbookCache;
            }
            catch (DataServiceTransportException e)
            {
                Core.Log.Error("Unable to retrieve the runbooks from SMA.", e);
                NotifyConnectionError();
                SuccessfulInitialization = false;

                return new List<Runbook>();
            }
        }

        public IList<string> GetTags()
        {
            if (_tagCache == null)
                _tagCache = new List<string>();
            else
                _tagCache.Clear();

            foreach (var runbook in _runbookCache)
            {
                if (runbook.Tags == null)
                    continue;

                var splitTags = runbook.Tags.Split(',');
                foreach (var tag in splitTags)
                {
                    var fixedTag = tag.Trim();

                    if (!_tagCache.Contains(fixedTag))
                        _tagCache.Add(fixedTag);
                }
            }

            _tagCache = _tagCache.OrderBy(t => t).ToList();

            return _tagCache;
        }

        public ObservableCollection<TagViewModel> GetTagViewModels()
        {
            //var list = new ObservableCollection<TagViewModel>();
            if (_tagViewModelCache == null)
                _tagViewModelCache = new ObservableCollection<TagViewModel>();

            GetTags();

            bool needToCache = false;
            foreach (var tag in _tagCache)
            {
                var tagViewModel = _tagViewModelCache.FirstOrDefault(t => t.Name.Equals(tag));

                if (tagViewModel == null)
                {
                    needToCache = true;
                    tagViewModel = new TagViewModel(tag);
                }
                else
                    needToCache = false;

                AsyncService.ExecuteOnUIThread(delegate()
                {
                    tagViewModel.Runbooks.Clear();
                    var tmp = _runbookViewModelCache.Where(r => r.Tags != null && r.Tags.Contains(tag)).ToObservableCollection();

                    foreach (var runbook in tmp)
                        tagViewModel.Runbooks.Add(runbook);
                    //tagViewModel.Runbooks = _runbookViewModelCache.Where(r => r.Tags != null && r.Tags.Contains(tag)).ToObservableCollection();

                    if (needToCache)
                        _tagViewModelCache.Add(tagViewModel);
                });
            }

            // Take care of untagged runbooks too
            var untaggedViewModel = _tagViewModelCache.FirstOrDefault(t => t.Name.Equals("(untagged)"));
            needToCache = false;

            if (untaggedViewModel == null)
            {
                untaggedViewModel = new TagViewModel("(untagged)");
                needToCache = true;
            }

            AsyncService.ExecuteOnUIThread(delegate()
            {
                untaggedViewModel.Runbooks.Clear();
                var tmp = _runbookViewModelCache.Where(r => r.Tags == null).ToObservableCollection();

                foreach (var runbook in tmp)
                    untaggedViewModel.Runbooks.Add(runbook);

                if (needToCache)
                {
                    _tagViewModelCache.Add(untaggedViewModel);
                }
            });

            return _tagViewModelCache;
        }

        public ObservableCollection<RunbookViewModel> GetRunbookViewModels(bool forceDownload = false)
        {
            if (_runbookCache == null || forceDownload)
                GetRunbooks(forceDownload);

            if (_runbookViewModelCache != null && !forceDownload)
                return _runbookViewModelCache;

            _runbookViewModelCache = new ObservableCollection<RunbookViewModel>();

            if (_runbookCache == null)
                return new ObservableCollection<RunbookViewModel>();

            foreach (var runbook in _runbookCache)
            {
                var viewModel = new RunbookViewModel
                {
                    Runbook = runbook,
                    CheckedOut = runbook.DraftRunbookVersionID.HasValue
                };

                _runbookViewModelCache.Add(viewModel);
            }

            return _runbookViewModelCache;
        }

        public List<RunbookVersionViewModel> GetVersions(RunbookViewModel runbookViewModel)
        {
            try
            {
                var versions = _api.Current.RunbookVersions.Where(rv => rv.RunbookID.Equals(runbookViewModel.Runbook.RunbookID) && !rv.IsDraft).ToList();
                var versionsViewModels = new List<RunbookVersionViewModel>();

                foreach (var version in versions)
                    versionsViewModels.Add(new RunbookVersionViewModel(version));

                return versionsViewModels;
            }
            catch (DataServiceTransportException e)
            {
                Core.Log.Error(String.Format("Unable to retrieve versions for runbook {0}", runbookViewModel.ID), e);
                base.NotifyConnectionError();
                SuccessfulInitialization = false;

                return new List<RunbookVersionViewModel>();
            }
        }

        public Runbook GetRunbook(string runbookName)
        {
            var runbook = _api.Current.Runbooks.Where(r => r.RunbookName.Equals(runbookName, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();

            if (runbook == null)
                Core.Log.InfoFormat("GetRunbook: A runbook named {0} was not found.", runbook.RunbookName);

            return runbook;
        }

        public Runbook GetRunbook(Guid runbookId)
        {
            var runbook = _api.Current.Runbooks.Where(r => r.RunbookID.Equals(runbookId)).FirstOrDefault();

            if (runbook == null)
                Core.Log.InfoFormat("GetRunbook: A runbook with ID {0} was not found.", runbook.RunbookName);

            return runbook;
        }

        public bool Create()
        {
            return Create(string.Empty);
        }

        public bool Create(string runbookName, string runbookContent = "")
        {
            try
            {
                var newRunbook = new RunbookViewModel
                {
                    Runbook = new SMAWebService.Runbook(),
                    CheckedOut = true,
                    UnsavedChanges = true,
                };

                // Set the name of the runbook
                newRunbook.Runbook.RunbookName = runbookName;

                // Set the runbook content as well
                newRunbook.Content = runbookContent;

                _workspaceViewModel.OpenDocument(newRunbook);

                return true;
            }
            catch (Exception ex)
            {
                Core.Log.Error("Unable to create a new runbook.", ex);
            }

            return false;
        }

        public bool Update(RunbookViewModel rb)
        {
            if (String.IsNullOrEmpty(rb.RunbookName) || rb.ID == Guid.Empty)
            {
                if (String.IsNullOrEmpty(rb.RunbookName))
                {
                    var window = new NewRunbookDialog();
                    window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    window.Topmost = true;
                    if (!(bool)window.ShowDialog())
                    {
                        // The user canceled the save
                        return false;
                    }
                }

                SaveNewRunbook(rb);
                return false;
            }

            var runbook = _api.Current.Runbooks.Where(r => r.RunbookID == rb.Runbook.RunbookID).FirstOrDefault();
            if (runbook == null)
            {
                MessageBox.Show("The runbook does not exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (!runbook.DraftRunbookVersionID.HasValue || runbook.DraftRunbookVersionID == Guid.Empty)
            {
                MessageBox.Show("The runbook is checked in and can therefore not be edited.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            try
            {
                MemoryStream ms = new MemoryStream();
                byte[] bytes = Encoding.UTF8.GetBytes(rb.Content);
                ms.Write(bytes, 0, bytes.Length);
                ms.Seek(0, SeekOrigin.Begin);

                Stream baseStream = (Stream)ms;
                RunbookVersion entity = (from rv in _api.Current.RunbookVersions
                                         where (Guid?)rv.RunbookVersionID == runbook.DraftRunbookVersionID
                                         select rv).FirstOrDefault<RunbookVersion>();

                _api.Current.SetSaveStream(entity, baseStream, true, "application/octet-stream", string.Empty);
                _api.Current.SaveChanges();

                runbook.Tags = rb.Runbook.Tags;
                runbook.Description = rb.Runbook.Description;

                _api.Current.UpdateObject(runbook);
                _api.Current.SaveChanges();

                rb.CheckedOut = true;
                rb.Runbook = runbook;

                rb.UnsavedChanges = false;
            }
            catch (Exception e)
            {
                Core.Log.Error("Unable to save a draft of the runbook.", e);
                MessageBox.Show("Something went wrong when trying to save the draft. Refer to the logs for more information.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }

        public bool Delete(RunbookViewModel runbookViewModel)
        {
            try
            {
                var runbook = _api.Current.Runbooks.Where(r => r.RunbookID == runbookViewModel.Runbook.RunbookID).FirstOrDefault();

                if (runbook == null)
                {
                    Core.Log.DebugFormat("Trying to remove a runbook that doesn't exist. GUID: {0}", runbookViewModel.Runbook.RunbookID);
                    return false;
                }

                _api.Current.DeleteObject(runbook);
                _api.Current.SaveChanges();

                // Remove the runbook from the list of runbooks
                if (_componentsViewModel != null)
                    _componentsViewModel.RemoveRunbook(runbookViewModel);

                // If the runbook is open, we close it
                if (_workspaceViewModel != null && _workspaceViewModel.Documents.Contains(runbookViewModel))
                    _workspaceViewModel.Documents.Remove(runbookViewModel);

                return true;
            }
            catch (Exception ex)
            {
                Core.Log.Error("Unable to remove the runbook.", ex);
                MessageBox.Show("An error occurred when trying to remove the runbook. Please refer to the logs for more information.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return false;
        }

        public bool CheckIn(RunbookViewModel runbookViewModel)
        {
            var runbook = runbookViewModel.Runbook;

            try
            {
                _api.Current.AttachTo("Runbooks", runbook);
            }
            catch (InvalidOperationException)
            {
                // Thrown when the context is already tracking the entity
            }

            if (!runbook.DraftRunbookVersionID.HasValue || runbook.DraftRunbookVersionID == Guid.Empty)
            {
                MessageBox.Show("The runbook's already checked in.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);

                // If we have gotten out of sync, mark this runbook as already checked in
                runbookViewModel.CheckedOut = false;

                return false;
            }

            try
            {
                // Publish the runbook
                runbookViewModel.Runbook.PublishedRunbookVersionID = runbook.Publish(_api.Current);
                runbookViewModel.Runbook.DraftRunbookVersionID = Guid.Empty;

                runbookViewModel.CheckedOut = false;

                runbookViewModel.Runbook = runbook;
            }
            catch (DataServiceClientException ex)
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(ex.Message);

                var errorMessage = xmlDoc.SelectSingleNode("//error/message").InnerText;

                Core.Log.Error("Error in runbook.", ex);
                MessageBox.Show(errorMessage, "Parse Error", MessageBoxButton.OK, MessageBoxImage.Error);

                return false;
            }
            catch (Exception e)
            {
                Core.Log.Error("Something went wrong when checking in the runbook.", e);
                MessageBox.Show("An error occurred when checking in the runbook. Refer to the logs for more information.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                return false;
            }

            return true;
        }

        public bool CheckOut(RunbookViewModel runbookViewModel, bool silentCheckOut = false)
        {
            var messageBoxResult = MessageBoxResult.Yes;

            if (!silentCheckOut)
                messageBoxResult = MessageBox.Show("Do you want to check out the runbook?\r\nThis will still allow the current version of the runbook to run.", "Question", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (messageBoxResult != MessageBoxResult.Yes)
                return false;

            var runbook = runbookViewModel.Runbook;

            try
            {
                _api.Current.AttachTo("Runbooks", runbook);
            }
            catch (InvalidOperationException)
            {
                // Thrown when we're already attached to the API
            }
            
            if (!runbook.DraftRunbookVersionID.HasValue || runbook.DraftRunbookVersionID == Guid.Empty)
            {
                runbook.DraftRunbookVersionID = new Guid?(runbook.Edit(_api.Current));
            }
            else
            {
                // TODO: Support overwriting of already checked out runbook?
                Core.Log.ErrorFormat("The runbook was already checked out.");
                MessageBox.Show("The runbook's already checked out.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            // First, we need to download the published code and then republish it as a draft
            // Retrieve the raw content of the runbook
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(SettingsManager.Current.Settings.SmaWebServiceUrl + "/Runbooks(guid'" + runbook.RunbookID + "')/PublishedRunbookVersion/$value");

            if (SettingsManager.Current.Settings.Impersonate)
            {
                request.Credentials = CredentialCache.DefaultCredentials;
            }
            else
            {
                request.Credentials = new NetworkCredential(SettingsManager.Current.Settings.UserName, SettingsManager.Current.Settings.GetPassword(), SettingsManager.Current.Settings.Domain);
            }           

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            TextReader reader = new StreamReader(response.GetResponseStream());

            string content = reader.ReadToEnd();

            reader.Close();

            runbookViewModel.Content = content;

            try
            {
                MemoryStream ms = new MemoryStream();
                byte[] bytes = Encoding.UTF8.GetBytes(runbookViewModel.Content);
                ms.Write(bytes, 0, bytes.Length);
                ms.Seek(0, SeekOrigin.Begin);

                Stream baseStream = (Stream)ms;
                RunbookVersion entity = (from rv in _api.Current.RunbookVersions
                                         where (Guid?)rv.RunbookVersionID == runbook.DraftRunbookVersionID
                                         select rv).FirstOrDefault<RunbookVersion>();

                _api.Current.SetSaveStream(entity, baseStream, true, "application/octet-stream", string.Empty);
                _api.Current.SaveChanges();

                runbookViewModel.CheckedOut = true;
                runbookViewModel.Runbook = _api.Current.Runbooks.Where(r => r.RunbookID == runbook.RunbookID).First();
                
                runbookViewModel.Versions = GetVersions(runbookViewModel);
                runbookViewModel.LoadedVersions = true;

                return true;
            }
            catch (Exception ex)
            {
                Core.Log.Error("Unable to check out runbook.", ex);
                MessageBox.Show("An error occurred when trying to check out the runbook. Please refer to the logs for more information.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return false;
        }

        /// <summary>
        /// Returns the Guid of the job for the selected runbook that is set in Suspended mode.
        /// </summary>
        /// <param name="runbook"></param>
        /// <returns></returns>
        public Guid GetSuspendedJobs(Runbook runbook)
        {
            // Use the cached value and only check towards the webservice every two minutes.
            // If we are within the two minute interval, use the last known value.
            if ((DateTime.Now - _lastCheckedSuspendedJobs).TotalMinutes < 2)
                return _lastSuspendedJobID;

            _lastCheckedSuspendedJobs = DateTime.Now;

            try
            {
                var jobContexts = _api.Current.JobContexts.Where(jc => jc.RunbookVersionID.Equals(runbook.DraftRunbookVersionID) || jc.RunbookVersionID.Equals(runbook.PublishedRunbookVersionID)).ToList();

                var jobs = _api.Current.Jobs.Where(j => j.JobStatus.Equals("Suspended")).ToList();
                foreach (var context in jobContexts)
                {
                    var job = jobs.Where(j => j.JobContextID.Equals(context.JobContextID)).FirstOrDefault();

                    if (job != null)
                    {
                        _lastSuspendedJobID = job.JobID;
                        return job.JobID;
                    }
                }
            }
            catch (DataServiceTransportException e)
            {
                Core.Log.Error("Unable to connect to the SMA webservice. Network connectivity lost?", e);
            }

            _lastSuspendedJobID = Guid.Empty;
            return Guid.Empty;
        }

        /// <summary>
        /// Returns the Guid of the job for the selected runbook that is set in a running mode.
        /// </summary>
        /// <param name="runbook"></param>
        /// <returns></returns>
        public Guid GetActiveJobs(Runbook runbook)
        {
            // Use the cached value and only check towards the webservice every two minutes.
            // If we are within the two minute interval, use the last known value.
            if ((DateTime.Now - _lastCheckedRunningJobs).TotalMinutes < 2)
                return _lastRunningJobID;

            _lastCheckedRunningJobs = DateTime.Now;

            try
            {
                var jobContexts = _api.Current.JobContexts.Where(jc => jc.RunbookVersionID.Equals(runbook.DraftRunbookVersionID) || jc.RunbookVersionID.Equals(runbook.PublishedRunbookVersionID)).ToList();

                var jobs = _api.Current.Jobs.Where(j => j.JobStatus.Equals("Suspended") || j.JobStatus.Equals("Running") || j.JobStatus.Equals("New")).ToList();
                foreach (var context in jobContexts)
                {
                    var job = jobs.Where(j => j.JobContextID.Equals(context.JobContextID)).FirstOrDefault();

                    if (job != null)
                    {
                        _lastRunningJobID = job.JobID;
                        return job.JobID;
                    }
                }
            }
            catch (DataServiceTransportException e)
            {
                Core.Log.Error("Unable to connect to the SMA webservice. Network connectivity lost?", e);
            }

            _lastRunningJobID = Guid.Empty;
            return Guid.Empty;
        }

        private void SaveNewRunbook(RunbookViewModel runbookViewModel)
        {
            var runbookVersion = new RunbookVersion
            {
                TenantID = new Guid("00000000-0000-0000-0000-000000000000"),
                IsDraft = true
            };

            _api.Current.AddToRunbookVersions(runbookVersion);

            MemoryStream ms = new MemoryStream();
            byte[] bytes = Encoding.UTF8.GetBytes(runbookViewModel.Content);
            ms.Write(bytes, 0, bytes.Length);
            ms.Seek(0, SeekOrigin.Begin);

            Stream baseStream = (Stream)ms;

            _api.Current.SetSaveStream(runbookVersion, baseStream, true, "application/octet-stream", string.Empty);

            EntityDescriptor ed = null;
            try
            {
                ChangeOperationResponse cor =
                    (ChangeOperationResponse)_api.Current.SaveChanges().FirstOrDefault<OperationResponse>();

                if (cor != null)
                {
                    ed = (cor.Descriptor as EntityDescriptor);
                }
            }
            catch (Exception e)
            {
                Core.Log.Error("Unable to verify the saved runbook.", e);
                MessageBox.Show("Sorry, we were unable to save your runbook. Please refer to the log for more information.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (ed != null && ed.EditLink != null)
            {
                MergeOption mergeOption = _api.Current.MergeOption;
                _api.Current.MergeOption = MergeOption.OverwriteChanges;
                try
                {
                    _api.Current.Execute<RunbookVersion>(ed.EditLink).Count<RunbookVersion>();
                }
                catch (Exception e)
                {
                    Core.Log.Error("Unable to save the runbook.", e);
                    MessageBox.Show("There was an error when saving the runbook. Please try again later.", "Error");
                    return;
                }
                finally
                {
                    _api.Current.MergeOption = mergeOption;
                }
            }

            var runbook = _api.Current.Runbooks.Where(r => r.RunbookID == runbookVersion.RunbookID).FirstOrDefault();

            if (runbook == null)
            {
                // there was some error when importing the runbook
                MessageBox.Show("There was an error when saving the runbook. Please refer to the log for more information.", "Error");
                return;
            }

            // If we have specified any tags for this runbook - we need to save them as well
            if (!String.IsNullOrEmpty(runbookViewModel.Tags))
            {
                runbook.Tags = runbookViewModel.Tags;
                _api.Current.UpdateObject(runbook);

                _api.Current.SaveChanges();
            }

            runbookViewModel.Runbook = runbook;
            runbookViewModel.CheckedOut = true;
            runbookViewModel.UnsavedChanges = false;

            if (!_componentsViewModel.Runbooks.Contains(runbookViewModel))
                _componentsViewModel.AddRunbook(runbookViewModel);

            // Reload all runbooks since we have saved a new one
            _componentsViewModel.Load(true /* force download */);
        }
    }
}
