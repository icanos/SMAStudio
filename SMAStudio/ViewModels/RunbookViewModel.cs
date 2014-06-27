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
using SMAStudio.Models;
using System.Collections.ObjectModel;
using SMAStudio.Editor.Parsing;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Snippets;

namespace SMAStudio.ViewModels
{
    public class RunbookViewModel : ObservableObject, IDocumentViewModel
    {
        private bool _checkedOut = true;
        private bool _unsavedChanges = false;

        private string _content = string.Empty;  // used for comparsion between the local copy and the remote (to detect changes)
        private string _icon = Icons.Runbook;
        private DateTime _lastFetched = DateTime.MinValue;

        private Runbook _runbook = null;
        private TextDocument _document = null;

        public RunbookViewModel()
        {
            Versions = new List<RunbookVersionViewModel>();
            References = new ObservableCollection<DocumentReference>();
            LoadedVersions = false;

            // The UI thread needs to own the document in order to be able
            // to edit it.
            if (App.Current == null)
                return;

            App.Current.Dispatcher.Invoke(delegate()
            {
                Document = new TextDocument();
            });

            
        }

        /// <summary>
        /// Event triggered when the text changes in the text editor when this runbook
        /// is active.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void TextChanged(object sender, EventArgs e)
        {
            if (!(sender is MvvmTextEditor))
                return;

            var editor = ((MvvmTextEditor)sender);
            if (editor.Document.Text.Equals(_content))
                return;

            //Content = ((MvvmTextEditor)sender).Text;
            UnsavedChanges = true;
        }

        /// <summary>
        /// Retrieve the content of the current runbook version
        /// </summary>
        /// <param name="forceDownload">Forces the application to download new content from the web service instead of using the cached information.</param>
        /// <param name="publishedVersion">Set to true if we want to download the published version of the runbook, otherwise we'll get the draft</param>
        /// <returns>The content of the runbook</returns>
        public string GetContent(bool forceDownload = false, bool publishedVersion = false)
        {
            if (!String.IsNullOrEmpty(Content) && !forceDownload && (DateTime.Now - _lastFetched) < new TimeSpan(0, 30, 0))
                return Content;

            string runbookVersion = "DraftRunbookVersion";
            if (publishedVersion)
                runbookVersion = "PublishedRunbookVersion";

            Core.Log.DebugFormat("Downloading content for runbook '{0}', version: {1}", ID, runbookVersion);

            try
            {
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(Uri.AbsoluteUri + "/" + runbookVersion + "/$value");
                request.Credentials = CredentialCache.DefaultCredentials;

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                TextReader reader = new StreamReader(response.GetResponseStream());

                Content = reader.ReadToEnd();
                _content = Content;

                reader.Close();
            }
            catch (WebException e)
            {
                Core.Log.Error("WebException received when trying to download content of runbook from SMA", e);

                if (e.Status != WebExceptionStatus.ConnectFailure &&
                    e.Status != WebExceptionStatus.ConnectionClosed)
                {
                    Content = GetContent(forceDownload, true);
                    _content = Content;
                }
            }

            return Content;
        }

        public void DocumentLoaded()
        {
            
        }

        #region Properties
        /// <summary>
        /// Contains a mapping to the Model object of our runbook
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
        /// Contains all versions of the runbook that has been checked in
        /// </summary>
        public List<RunbookVersionViewModel> Versions { get; set; }

        /// <summary>
        /// A mapping to the Runbook ID
        /// </summary>
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

        public TextDocument Document
        {
            get { return _document; }
            set
            {
                _document = value;
            }
        }

        /// <summary>
        /// Data bound to the TextArea of AvalonEdit
        /// </summary>
        public TextArea MvvmTextArea
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the content of the Runbook (the actual Powershell script)
        /// </summary>
        public string Content
        {
            get
            {
                string content = "";

                // App is closing
                if (App.Current == null)
                    return string.Empty;

                App.Current.Dispatcher.Invoke(delegate()
                {
                    content = Document.Text;
                });

                return content;
            }
            set
            {
                // App is closing
                if (App.Current == null)
                    return;

                App.Current.Dispatcher.Invoke(delegate()
                {
                    Document.Text = value;
                });

                base.RaisePropertyChanged("Document");

                if (!_content.Equals(value))
                    base.RaisePropertyChanged("UnsavedChanges");
            }
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

        /// <summary>
        /// Set to true if the runbook contains changes that are cached and not saved
        /// </summary>
        public bool CachedChanges
        {
            get;
            set;
        }

        /// <summary>
        /// Set to true if versions has been loaded
        /// </summary>
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

        /// <summary>
        /// Last DateTime a key was pressed in the text editor of this runbook instance
        /// </summary>
        public DateTime LastTimeKeyDown
        {
            get;
            set;
        }

        /// <summary>
        /// Set to the ID of the job if the runbook is currently executed otherwise Guid.Empty
        /// </summary>
        public Guid JobID
        {
            get;
            set;
        }

        public ObservableCollection<DocumentReference> References
        {
            get;
            set;
        }

        public int CaretOffset
        {
            get;
            set;
        }
        #endregion

        /// <summary>
        /// Returns the Runbook Name
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return RunbookName;
        }
    }
}
