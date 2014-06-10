using SMAStudio.Commands;
using SMAStudio.Util;
using SMAStudio.SMAWebService;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using SMAStudio.Resources;
using SMAStudio.Editor;
using SMAStudio.Settings;

namespace SMAStudio.ViewModels
{
    public class RunbookViewModel : ObservableObject, IDocumentViewModel
    {
        private bool _checkedOut = true;
        private bool _unsavedChanges = false;

        private string _content = string.Empty;
        private string _icon = Icons.Runbook;
        private DateTime _lastFetched = DateTime.MinValue;

        private Runbook _runbook = null;

        public RunbookViewModel()
        {
            Versions = new List<RunbookVersionViewModel>();
            LoadedVersions = false;
        }

        public void TextChanged(object sender, EventArgs e)
        {
            if (!(sender is MvvmTextEditor))
                return;

            if (Content.Equals(((MvvmTextEditor)sender).Text))
                return;

            Content = ((MvvmTextEditor)sender).Text;
            UnsavedChanges = true;
        }

        /// <summary>
        /// Retrieve the content of the current runbook version
        /// </summary>
        /// <param name="forceDownload">Forces the application to download new content from the web service instead of using the cached information.</param>
        /// <returns></returns>
        public string GetContent(bool forceDownload = false, bool publishedVersion = false)
        {
            if (!String.IsNullOrEmpty(_content) && !forceDownload && (DateTime.Now - _lastFetched) < new TimeSpan(0, 30, 0))
                return _content;

            string runbookVersion = "DraftRunbookVersion";
            if (publishedVersion)
                runbookVersion = "PublishedRunbookVersion";

            try
            {
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(Uri.AbsoluteUri + "/" + runbookVersion + "/$value");
                request.Credentials = CredentialCache.DefaultCredentials;

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                TextReader reader = new StreamReader(response.GetResponseStream());

                _content = reader.ReadToEnd();

                reader.Close();
            }
            catch (WebException)
            {
                // 404...
                _content = GetContent(forceDownload, true);
            }

            return _content;
        }

        #region Properties
        /// <summary>
        /// Contains the actual runbook from SMA
        /// </summary>
        public Runbook Runbook
        {
            get { return _runbook; }
            set
            {
                _runbook = value;
                base.RaisePropertyChanged("Title");
                base.RaisePropertyChanged("RunbookName");
            }
        }

        /// <summary>
        /// Different versions of this runbook
        /// </summary>
        public List<RunbookVersionViewModel> Versions { get; set; }

        public Guid ID
        {
            get { return Runbook.RunbookID; }
            set { Runbook.RunbookID = value; }
        }

        /// <summary>
        /// Title to be shown in the tab and treeview. Will contain (draft) if it's not yet published
        /// and a * if the Runbook contains unsaved work.
        /// </summary>
        public string Title
        {
            get
            {
                string runbookName = (Runbook != null) ? Runbook.RunbookName : string.Empty;

                if (String.IsNullOrEmpty(runbookName))
                    runbookName = "untitled";

                if (UnsavedChanges)
                    runbookName += "*";

                if (CheckedOut)
                    runbookName += " (draft)";

                return runbookName;
            }
        }

        /// <summary>
        /// Name of the Runbook
        /// </summary>
        public string RunbookName
        {
            get
            {
                return (Runbook != null) ? Runbook.RunbookName : "";
            }
        }

        /// <summary>
        /// Gets or sets the content of the Runbook (the actual Powershell script)
        /// </summary>
        public string Content
        {
            get { return _content; }
            set { _content = value; }
        }

        /// <summary>
        /// Gets or sets whether or not the Runbook is checked out
        /// </summary>
        public bool CheckedOut {
            get { return _checkedOut; }
            set { _checkedOut = value; base.RaisePropertyChanged("CheckedIn"); }
        }

        /// <summary>
        /// Gets the opposite of CheckedOut
        /// </summary>
        public bool CheckedIn
        {
            get { return !CheckedOut; }
        }

        /// <summary>
        /// Gets or sets whether this Runbook contains unsaved work
        /// </summary>
        public bool UnsavedChanges
        {
            get { return _unsavedChanges; }
            set
            {
                _unsavedChanges = value;

                // Set the CachedChanges to false in order for our auto saving engine to store a
                // local copy in case the application crashes
                CachedChanges = false;

                base.RaisePropertyChanged("Title");
            }
        }

        public bool CachedChanges
        {
            get;
            set;
        }

        public bool LoadedVersions
        {
            get;
            set;
        }

        /// <summary>
        /// Returns the string of Tags defined on the runbook
        /// </summary>
        public string Tags
        {
            get { return Runbook != null ? Runbook.Tags : ""; }
            set { Runbook.Tags = value; UnsavedChanges = true; }
        }

        /// <summary>
        /// Gets the Uri of the Runbook
        /// </summary>
        public Uri Uri
        {
            get
            {
                return new Uri(SettingsManager.Current.Settings.SmaWebServiceUrl + "/Runbooks(guid'" + Runbook.RunbookID + "')");
            }
        }

        /// <summary>
        /// Icon for a Runbook
        /// </summary>
        public string Icon
        {
            get { return _icon; }
            set { _icon = value; base.RaisePropertyChanged("Icon"); }
        }

        public DateTime LastTimeKeyDown
        {
            get;
            set;
        }

        public Guid JobID
        {
            get;
            set;
        }
        #endregion


        public override string ToString()
        {
            return RunbookName;
        }
    }
}
