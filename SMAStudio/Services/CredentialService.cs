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

namespace SMAStudio.Services
{
    public class CredentialService : BaseService
    {
        private ApiService _api;
        private IList<Credential> _credentialCache = null;
        private ObservableCollection<CredentialViewModel> _credentialViewModelCache = null;

        public CredentialService()
        {
            _api = new ApiService();
        }

        public IList<Credential> GetCredentials(bool forceDownload=false)
        {
            try
            {
                if (_credentialCache == null || forceDownload)
                    _credentialCache = _api.Current.Credentials.OrderBy(c => c.Name).ToList();

                return _credentialCache;
            }
            catch (DataServiceTransportException)
            {
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
    }
}
