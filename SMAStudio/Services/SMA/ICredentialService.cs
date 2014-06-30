using SMAStudio.SMAWebService;
using SMAStudio.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudio.Services
{
    public interface ICredentialService
    {
        IList<Credential> GetCredentials(bool forceDownload = false);

        ObservableCollection<CredentialViewModel> GetCredentialViewModels(bool forceDownload = false);

        bool Create();

        bool Update(CredentialViewModel credential);

        bool Delete(CredentialViewModel credential);
    }
}
