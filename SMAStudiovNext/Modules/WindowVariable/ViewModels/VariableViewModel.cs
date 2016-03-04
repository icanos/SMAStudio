using Gemini.Framework;
using Gemini.Framework.Commands;
using SMAStudiovNext.Core;
using SMAStudiovNext.Models;
using SMAStudiovNext.Modules.Shell.Commands;
using SMAStudiovNext.Services;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace SMAStudiovNext.Modules.Variable.ViewModels
{
    public sealed class VariableViewModel : Document, IViewModel, ICommandHandler<SaveCommandDefinition>
    {
        private readonly VariableModelProxy model;
        private string value;

        public VariableViewModel(VariableModelProxy variable)
        {
            model = variable;

            if (variable.VariableID == Guid.Empty)
            {
                UnsavedChanges = true;
            }

            if (variable.IsEncrypted)
                value = "<Encrypted value>";
            else
                value = JsonConverter.FromJson(variable.Value).ToString();

            Owner = variable.Context.Service;
        }

        public override void CanClose(Action<bool> callback)
        {
            if (UnsavedChanges)
            {
                var result = MessageBox.Show("There are unsaved changes in the variable object, changes will be lost. Do you want to continue?", "Unsaved changes", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                {
                    callback(false);
                    return;
                }
            }

            callback(true);
        }

        #region Properties
        public override string DisplayName
        {
            get
            {
                if (!UnsavedChanges)
                {
                    if (String.IsNullOrEmpty(Name))
                        return "(unnamed)";

                    return Name;
                }

                if (String.IsNullOrEmpty(Name))
                    return "(unnamed)*";

                return Name + "*";
            }
        }

        public string Name
        {
            get
            {
                return model.Name;
            }
            set
            {
                model.Name = value;
                NotifyOfPropertyChange(() => DisplayName);
            }
        }

        public string Content
        {
            get
            {
                return value;
            }
            set
            {
                this.value = value;
                UnsavedChanges = true;

                NotifyOfPropertyChange(() => DisplayName);
            }
        }

        public bool IsEncrypted
        {
            get
            {
                return (bool)model.IsEncrypted;
            }
            set
            {
                model.IsEncrypted = value;
                UnsavedChanges = true;

                NotifyOfPropertyChange(() => DisplayName);
            }
        }

        public object Model
        {
            get
            {
                return model;
            }
            set
            {
                // Cannot be assigned
                throw new NotSupportedException();
            }
        }

        public bool UnsavedChanges
        {
            get; set;
        }

        public IBackendService Owner
        {
            private get;
            set;
        }

        #endregion

        void ICommandHandler<SaveCommandDefinition>.Update(Command command)
        {
            if (UnsavedChanges)
                command.Enabled = true;
            else
                command.Enabled = false;
        }

        async Task ICommandHandler<SaveCommandDefinition>.Run(Command command)
        {
            await Task.Run(delegate ()
            {
                model.Value = JsonConverter.ToJson(value);
                model.ViewModel = this;

                try {
                    Owner.Save(this, command);
                    Owner.Context.AddToVariables(model);
                }
                catch (ApplicationException ex)
                {
                    GlobalExceptionHandler.Show(ex);
                }

                // Update the UI to notify that the changes has been saved
                UnsavedChanges = false;
                NotifyOfPropertyChange(() => DisplayName);
            });
        }
    }
}
