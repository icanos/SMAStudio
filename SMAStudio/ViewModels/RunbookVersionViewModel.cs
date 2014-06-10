using SMAStudio.Settings;
using SMAStudio.SMAWebService;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudio.ViewModels
{
    public class RunbookVersionViewModel
    {
        public RunbookVersionViewModel()
        {
            
        }

        public RunbookVersionViewModel(RunbookVersion runbookVersion)
        {
            RunbookVersion = runbookVersion;
        }

        public RunbookVersion RunbookVersion
        {
            get;
            set;
        }

        public int VersionNumber
        {
            get { return RunbookVersion.VersionNumber; }
        }

        public DateTime Created
        {
            get { return RunbookVersion.CreationTime; }
        }

        public Uri Uri
        {
            get
            {
                return new Uri(SettingsManager.Current.Settings.SmaWebServiceUrl + "/RunbookVersions(guid'" + RunbookVersion.RunbookVersionID + "')");
            }
        }

        private string _content = string.Empty;
        private DateTime _lastFetched = DateTime.MinValue;

        /// <summary>
        /// Retrieve the content of the current runbook version
        /// </summary>
        /// <param name="forceDownload">Forces the application to download new content from the web service instead of using the cached information.</param>
        /// <returns></returns>
        public string GetContent(bool forceDownload = false)
        {
            if (!String.IsNullOrEmpty(_content) && !forceDownload &&  (DateTime.Now - _lastFetched) < new TimeSpan(0, 30, 0))
                return _content;

            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(Uri.AbsoluteUri + "/$value");
            request.Credentials = CredentialCache.DefaultCredentials;

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            TextReader reader = new StreamReader(response.GetResponseStream());

            _content = reader.ReadToEnd();

            reader.Close();

            return _content;
        }
    }
}
