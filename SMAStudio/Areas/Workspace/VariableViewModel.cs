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
    public class VariableViewModel : ObservableObject, IDocumentViewModel
    {
        private bool _unsavedChanges = false;
        private string _icon = Icons.Variable;
        private Variable _variable = null;

        /// <summary>
        /// Store the value of the variable in a separate variable
        /// so that we can, when saving, convert it to JSON to make
        /// sure we're not breaking SMA :)
        /// </summary>
        private string _variableValue = "";

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

            TextBox textBox = (TextBox)sender;
            bool isNameBox = textBox.Tag.Equals("Name") ? true : false;

            // We assume that the value hasn't changed if the CaretIndex = 0
            if (textBox.CaretIndex == 0 && textBox.Text.Length > 0)
                return;

            if (isNameBox)
            {
                if (!textBox.Text.Equals(Name))
                {
                    Name = textBox.Text;
                    UnsavedChanges = true;
                }
            }
            else
            {
                if (!textBox.Text.Equals(Variable.Value))
                {
                    Content = textBox.Text;
                    UnsavedChanges = true;
                }
            }
        }

        public void DocumentLoaded()
        {
            
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
                _variableValue = JsonConverter.FromJson(_variable.Value).ToString();

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
            get { return _variableValue; }
            set { _variableValue = value; }
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
                base.RaisePropertyChanged("Name");
                base.RaisePropertyChanged("Title");

                if (_unsavedChanges.Equals(value))
                    return;

                _unsavedChanges = value;

                // Set the CachedChanges to false in order for our auto saving engine to store a
                // local copy in case the application crashes
                CachedChanges = false;
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

        /// <summary>
        /// Unused for variables
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
