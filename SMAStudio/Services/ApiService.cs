/* Copyright 2014 Marcus Westin

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.*/

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
    public class ApiService : IApiService
    {
        private OrchestratorApi _api;

        public ApiService()
        {
            ServicePointManager.ServerCertificateValidationCallback = ValidateServerCertficate;

            //_api = new OrchestratorApi(new Uri(ConfigurationManager.AppSettings["SMAApiUrl"]));
            _api = new OrchestratorApi(new Uri(SettingsManager.Current.Settings.SmaWebServiceUrl));
            ((DataServiceContext)_api).Credentials = CredentialCache.DefaultCredentials;
        }

        /// <summary>
        /// Tests connectivity against the SMA service
        /// </summary>
        /// <returns></returns>
        public bool TestConnectivity()
        {
            try
            {
                var runbook = _api.Runbooks.FirstOrDefault();
            }
            catch (DataServiceQueryException e)
            {
                Core.Log.Error("Unable to connect to SMA. Verify the URL and/or credentials.", e);
                return false;
            }

            return true;
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
