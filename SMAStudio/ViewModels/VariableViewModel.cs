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

        /// <summary>
        /// Event triggered when the text changes in the text editor when this variable
        /// is active.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void TextChanged(object sender, EventArgs e)
        {
            if (!(sender is TextBox))
                return;

            if (Content.Equals(((TextBox)sender).Text))
                return;

            Content = ((TextBox)sender).Text;
            UnsavedChanges = true;
        }

        #region Properties
        /// <summary>
        /// Gets or sets the variable model object
        /// </summary>
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

        /// <summary>
        /// Gets or sets the ID of the variable
        /// </summary>
        public Guid ID
        {
            get { return Variable.VariableID; }
            set { Variable.VariableID = value; }
        }

        /// <summary>
        /// Gets the variable name accompanied wth a asterisk (*) if the variable contains
        /// unsaved data
        /// </summary>
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

        /// <summary>
        /// Gets or sets the name of the variable
        /// </summary>
        public string Name
        {
            get
            {
                return Variable.Name;
            }
            set { Variable.Name = value; }
        }

        /// <summary>
        /// Gets or sets the value of the variable
        /// </summary>
        public string Content
        {
            get { return Variable.Value; }
            set { Variable.Value = value; }
        }

        /// <summary>
        /// Gets or sets wether or not this variable is encrypted
        /// </summary>
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

        /// <summary>
        /// Set to true if the runbook contains changes that are cached and not saved
        /// </summary>
        public bool CachedChanges
        {
            get;
            set;
        }

        /// <summary>
        /// Will always return true since a variable is always checked out
        /// </summary>
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

        /// <summary>
        /// Last DateTime a key was pressed in the text editor of this runbook instance
        /// </summary>
        public DateTime LastTimeKeyDown
        {
            get;
            set;
        }
        #endregion
    }
}
