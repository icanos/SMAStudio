using SMAStudio.Settings;
using SMAStudio.SMAWebService;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Services.Client;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudio.Util
{
    sealed class ApiService
    {
        private OrchestratorApi _api;

        public ApiService()
        {
            ServicePointManager.ServerCertificateValidationCallback = ValidateServerCertficate;

            //_api = new OrchestratorApi(new Uri(ConfigurationManager.AppSettings["SMAApiUrl"]));
            _api = new OrchestratorApi(new Uri(SettingsManager.Current.Settings.SmaWebServiceUrl));
            ((DataServiceContext)_api).Credentials = CredentialCache.DefaultCredentials;
        }

        private bool ValidateServerCertficate(
                object sender,
                X509Certificate cert,
                X509Chain chain,
                SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        public OrchestratorApi Current
        {
            get
            {
                return _api;
            }
            private set { _api = value; }
        }
    }
}
