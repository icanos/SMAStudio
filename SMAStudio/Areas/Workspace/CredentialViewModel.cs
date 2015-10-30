using SMAStudio.Models;
using SMAStudio.Resources;
using SMAStudio.SMAWebService;
using SMAStudio.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace SMAStudio.ViewModels
{
    public class CredentialViewModel : ObservableObject, IDocumentViewModel
    {
        private bool _unsavedChanges = false;
        private string _icon = Icons.Credential;
        private Credential _credential = null;

        public CredentialViewModel()
        {

        }

        /// <summary>
        /// Event called when text changes in the credential object
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void TextChanged(object sender, EventArgs e)
        {
            if (!(sender is TextBox))
                return;

            var textBox = (TextBox)sender;

            if (textBox.Name.Equals("txtName") && !textBox.Text.Equals(Name))
                UnsavedChanges = true;
            else if (textBox.Name.Equals("txtUsername") && !textBox.Text.Equals(Username))
                UnsavedChanges = true;
        }

        public void DocumentLoaded()
        {
            
        }

        #region Properties
        public Credential Credential
        {
            get { return _credential; }
            set { _credential = value; base.RaisePropertyChanged("Title"); base.RaisePropertyChanged("Name"); }
        }

        public Guid ID
        {
            get { return _credential.CredentialID; }
            set { _credential.CredentialID = value; }
        }

        public string Title
        {
            get
            {
                string credentialName = Credential.Name;

                if (String.IsNullOrEmpty(credentialName))
                    credentialName += "untitled";

                if (UnsavedChanges)
                    credentialName += "*";

                return credentialName;
            }
            set { Credential.Name = value; }
        }

        public string Username
        {
            get { return _credential.UserName; }
            set { _credential.UserName = value; }
        }

        public string Password
        {
            get;
            set;
        }

        public string Name
        {
            get { return _credential.Name; }
            set { _credential.Name = value; }
        }

        public bool UnsavedChanges
        {
            get
            {
                return _unsavedChanges;
            }
            set
            {
                if (_unsavedChanges.Equals(value))
                    return;
                
                _unsavedChanges = value;

                // Set the CachedChanges to false in order for our auto saving engine to store a
                // local copy in case the application crashes
                CachedChanges = false;

                base.RaisePropertyChanged("Name");
                base.RaisePropertyChanged("Title");
            }
        }

        public bool CachedChanges
        {
            get;
            set;
        }

        public bool CheckedOut
        {
            get { return true; }
            set { }
        }

        /// <summary>
        /// Icon for a variable
        /// </summary>
        public string Icon
        {
            get { return _icon; }
            set { _icon = value; }
        }

        public DateTime LastTimeKeyDown
        {
            get;
            set;
        }

        public string Content
        {
            get
            {
                return _credential.Name;
            }
            set
            {
                _credential.Name = value;
            }
        }

        /// <summary>
        /// Unused for credentials
        /// </summary>
        public ObservableCollection<DocumentReference> References
        {
            get;
            set;
        }

        public bool IsExpanded
        {
            get;
            set;
        }
        #endregion
    }
}
