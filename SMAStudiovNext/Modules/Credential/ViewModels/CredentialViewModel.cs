using Gemini.Framework;
using Gemini.Framework.Commands;
using SMAStudiovNext.Core;
using SMAStudiovNext.Models;
using SMAStudiovNext.Modules.Shell.Commands;
using SMAStudiovNext.Services;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace SMAStudiovNext.Modules.Credential.ViewModels
{
    public sealed class CredentialViewModel : Document, IViewModel, ICommandHandler<SaveCommandDefinition>
    {
        private readonly CredentialModelProxy model;

        public CredentialViewModel(CredentialModelProxy credential)
        {
            model = credential;//new CredentialModelProxy(credential);

            if (credential.CredentialID == Guid.Empty)
            {
                UnsavedChanges = true;
            }

            Owner = credential.Context.Service;
        }

        public override void CanClose(Action<bool> callback)
        {
            if (UnsavedChanges)
            {
                var result = MessageBox.Show("There are unsaved changes in the credential object, changes will be lost. Do you want to continue?", "Unsaved changes", MessageBoxButton.YesNo, MessageBoxImage.Question);

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

        public string UserName
        {
            get
            {
                return model.UserName;
            }
            set
            {
                if (model.UserName != null && !model.UserName.Equals(value))
                    UnsavedChanges = true;

                model.UserName = value;

                NotifyOfPropertyChange(() => DisplayName);
            }
        }

        public string Password
        {
            get
            {
                return model.RawValue;
            }
            set
            {
                if (model.RawValue != null && !model.RawValue.Equals(value))
                    UnsavedChanges = true;

                model.RawValue = value;

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

        public string Content
        {
            get
            {
                return string.Empty;
            }
            set
            {
                /*model.Value = JsonConverter.ToJson(value);
                UnsavedChanges = true;

                NotifyOfPropertyChange(() => DisplayName);*/
            }
        }

        public IBackendService Owner
        {
            private get;
            set;
        }

        #endregion

        async Task ICommandHandler<SaveCommandDefinition>.Run(Command command)
        {
            await Task.Run(delegate ()
            {
                Owner.Save(this);

                model.ViewModel = this;

                //var backendContext = AppContext.Resolve<IBackendContext>();
                //backendContext.AddToCredentials(model);
                Owner.Context.AddToCredentials(model);

                // Update the UI to notify that the changes has been saved
                UnsavedChanges = false;
                NotifyOfPropertyChange(() => DisplayName);
            });
        }

        void ICommandHandler<SaveCommandDefinition>.Update(Command command)
        {
            if (UnsavedChanges)
                command.Enabled = true;
            else
                command.Enabled = false;
        }
    }
}
