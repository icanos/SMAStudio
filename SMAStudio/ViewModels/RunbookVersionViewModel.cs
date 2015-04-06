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
        private string _content = string.Empty;
        private DateTime _lastFetched = DateTime.MinValue;

        public RunbookVersionViewModel()
        {
            
        }

        public RunbookVersionViewModel(RunbookVersion runbookVersion)
        {
            RunbookVersion = runbookVersion;
        }

        /// <summary>
        /// Retrieve the content of the current runbook version
        /// </summary>
        /// <param name="forceDownload">Forces the application to download new content from the web service instead of using the cached information.</param>
        /// <returns></returns>
        public string GetContent(bool forceDownload = false)
        {
            if (!String.IsNullOrEmpty(_content) && !forceDownload &&  (DateTime.Now - _lastFetched) < new TimeSpan(0, 30, 0))
                return _content;

            Core.Log.DebugFormat("Downloading runbook version content from SMA");

            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(Uri.AbsoluteUri + "/$value");
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

            _content = reader.ReadToEnd();

            reader.Close();

            return _content;
        }

        #region Properties
        /// <summary>
        /// Gets or sets the RunbookVersion model object
        /// </summary>
        public RunbookVersion RunbookVersion
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the runbook version number
        /// </summary>
        public int VersionNumber
        {
            get { return RunbookVersion.VersionNumber; }
        }

        /// <summary>
        /// Gets the date when the version was created
        /// </summary>
        public DateTime Created
        {
            get { return RunbookVersion.CreationTime; }
        }

        /// <summary>
        /// Gets the URL to where the data of this version is contained
        /// </summary>
        public Uri Uri
        {
            get
            {
                return new Uri(SettingsManager.Current.Settings.SmaWebServiceUrl + "/RunbookVersions(guid'" + RunbookVersion.RunbookVersionID + "')");
            }
        }
        #endregion
    }
}
