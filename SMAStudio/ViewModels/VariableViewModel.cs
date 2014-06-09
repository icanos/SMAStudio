using SMAStudio.Resources;
using SMAStudio.SMAWebService;
using SMAStudio.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace SMAStudio.ViewModels
{
    public class VariableViewModel : ObservableObject, IDocumentViewModel
    {
        private bool _unsavedChanges = false;
        private string _icon = Icons.Variable;
        private Variable _variable = null;

        public VariableViewModel()
        {

        }

        public void TextChanged(object sender, EventArgs e)
        {
            if (!(sender is TextBox))
                return;

            if (Content.Equals(((TextBox)sender).Text))
                return;

            Content = ((TextBox)sender).Text;
            UnsavedChanges = true;
        }
        
        public Variable Variable
        {
            get { return _variable; }
            set
            {
                _variable = value;
                base.RaisePropertyChanged("Title");
                base.RaisePropertyChanged("Name");
            }
        }

        public Guid ID
        {
            get { return Variable.VariableID; }
            set { Variable.VariableID = value; }
        }

        public string Title
        {
            get
            {
                string variableName = Variable.Name;

                if (String.IsNullOrEmpty(variableName))
                    variableName += "untitled";

                if (UnsavedChanges)
                    variableName += "*";                

                return variableName;
            }
            set { Variable.Name = value; }
        }

        public string Name
        {
            get
            {
                return Variable.Name;
            }
            set { Variable.Name = value; }
        }

        public string Content
        {
            get { return Variable.Value; }
            set { Variable.Value = value; }
        }

        public bool IsEncrypted
        {
            get { return Variable.IsEncrypted; }
            set { Variable.IsEncrypted = value; }
        }

        /// <summary>
        /// Gets or sets whether this Runbook contains unsaved work
        /// </summary>
        public bool UnsavedChanges
        {
            get { return _unsavedChanges; }
            set
            {
                if (_unsavedChanges.Equals(value))
                    return;

                _unsavedChanges = value;

                // Set the CachedChanges to false in order for our auto saving engine to store a
                // local copy in case the application crashes
                CachedChanges = false;

                base.RaisePropertyChanged("Name");
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
    }
}
