using SMAStudio.SMAWebService;
using SMAStudio.Util;
using SMAStudio.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Services.Client;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SMAStudio.Services
{
    public class CredentialService : BaseService, ICredentialService
    {
        private IApiService _api;
        private IList<Credential> _credentialCache = null;
        private ObservableCollection<CredentialViewModel> _credentialViewModelCache = null;

        private IWorkspaceViewModel _workspaceViewModel;
        private IComponentsViewModel _componentsViewModel;

        public CredentialService()
        {
            _api = Core.Resolve<IApiService>();
            _workspaceViewModel = Core.Resolve<IWorkspaceViewModel>();
            _componentsViewModel = Core.Resolve<IComponentsViewModel>();
        }

        public IList<Credential> GetCredentials(bool forceDownload=false)
        {
            try
            {
                if (_credentialCache == null || forceDownload)
                    _credentialCache = _api.Current.Credentials.OrderBy(c => c.Name).ToList();

                return _credentialCache;
            }
            catch (DataServiceTransportException e)
            {
                Core.Log.Error("Unable to retrieve credentials from SMA", e);
                NotifyConnectionError();

                return new List<Credential>();
            }
        }

        public ObservableCollection<CredentialViewModel> GetCredentialViewModels(bool forceDownload = false)
        {
            if (_credentialCache == null || forceDownload)
                GetCredentials(forceDownload);

            if (_credentialViewModelCache != null && !forceDownload)
                return _credentialViewModelCache;

            _credentialViewModelCache = new ObservableCollection<CredentialViewModel>();

            if (_credentialViewModelCache == null)
                return new ObservableCollection<CredentialViewModel>();

            if (_credentialCache == null)
                return new ObservableCollection<CredentialViewModel>();

            foreach (var credential in _credentialCache)
            {
                var viewModel = new CredentialViewModel
                {
                    Credential = credential
                };

                _credentialViewModelCache.Add(viewModel);
            }

            return _credentialViewModelCache;
        }

        public bool Create()
        {
            try
            {
                var newCredential = new CredentialViewModel
                {
                    Credential = new SMAWebService.Credential(),
                    CheckedOut = true,
                    UnsavedChanges = true
                };

                _workspaceViewModel.OpenDocument(newCredential);

                return true;
            }
            catch (Exception ex)
            {
                Core.Log.Error("Unable to create a new credential.", ex);
            }

            return false;
        }

        public bool Update(CredentialViewModel credential)
        {
            Credential cred = null;

            try
            {
                if (credential.Credential.CredentialID != Guid.Empty)
                {
                    cred = _api.Current.Credentials.Where(c => c.CredentialID == credential.ID).FirstOrDefault();

                    if (cred == null)
                        return false;

                    cred.Name = credential.Name;
                    cred.UserName = credential.Username;
                    cred.RawValue = credential.Password;

                    _api.Current.UpdateObject(cred);
                    _api.Current.SaveChanges();
                }
                else
                {
                    cred = new Credential();

                    cred.Name = credential.Name;
                    cred.UserName = credential.Username;
                    cred.RawValue = credential.Password;

                    _api.Current.AddToCredentials(cred);
                    _api.Current.SaveChanges();

                    credential.Credential = cred;
                }

                credential.UnsavedChanges = false;
                credential.CachedChanges = false;

                if (!_componentsViewModel.Credentials.Contains(credential))
                    _componentsViewModel.Credentials.Add(credential);

                return true;
            }
            catch (Exception ex)
            {
                Core.Log.Error("Unable to save the credential.", ex);
                MessageBox.Show("An error occurred when saving the credential. Please refer to the logs for more information.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return false;
        }

        public bool Delete(CredentialViewModel credentialViewModel)
        {
            try
            {
                var credential = _api.Current.Credentials.Where(c => c.CredentialID == credentialViewModel.ID).FirstOrDefault();

                if (credential == null)
                {
                    Core.Log.DebugFormat("Trying to remove a credential that doesn't exist. GUID {0}", credentialViewModel.ID);
                    return false;
                }

                _api.Current.DeleteObject(credential);
                _api.Current.SaveChanges();

                if (_componentsViewModel != null)
                    _componentsViewModel.Credentials.Remove(credentialViewModel);

                if (_workspaceViewModel != null && _workspaceViewModel.Documents.Contains(credentialViewModel))
                    _workspaceViewModel.Documents.Remove(credentialViewModel);

                return true;
            }
            catch (Exception ex)
            {
                Core.Log.Error("Unable to remove the credential.", ex);
                MessageBox.Show("An error occurred when trying to remove the credential. Please refer to the logs for more information.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return false;
        }
    }
}
