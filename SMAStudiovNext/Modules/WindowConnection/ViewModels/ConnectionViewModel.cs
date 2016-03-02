using Caliburn.Micro;
using Gemini.Framework;
using Gemini.Framework.Commands;
using SMAStudiovNext.Core;
using SMAStudiovNext.Models;
using SMAStudiovNext.Modules.Shell.Commands;
using SMAStudiovNext.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Modules.WindowConnection.ViewModels
{
    public class ConnectionViewModel : Document, IViewModel, ICommandHandler<SaveCommandDefinition>
    {
        private readonly ConnectionTypeModelProxy model;

        public ConnectionViewModel(ConnectionTypeModelProxy connection)
        {
            model = connection;

            if (String.IsNullOrEmpty(connection.Name))
            {
                UnsavedChanges = true;
            }
            
            Owner = connection.Context.Service;
            ConnectionTypes = new ObservableCollection<ConnectionTypeModelProxy>();
            Parameters = new ObservableCollection<ConnectionViewParameter>();

            Task.Run(() =>
            {
                var result = Owner.GetConnectionTypes();

                Execute.OnUIThread(() =>
                {
                    foreach (var type in result)
                        ConnectionTypes.Add(type);
                });
            });
        }

        #region Properties
        public string Description { get; set; }

        public ObservableCollection<ConnectionTypeModelProxy> ConnectionTypes { get; set; }

        private ConnectionTypeModelProxy _connectionType;
        public ConnectionTypeModelProxy ConnectionType
        {
            get { return _connectionType; }
            set
            {
                _connectionType = value;
                Parameters.Clear();

                // Determine if SMA or Azure
                if (Owner is AzureService)
                {
                    // Azure
                    var connectionFields = (IList<Vendor.Azure.ConnectionField>)_connectionType.ConnectionFields;
                    foreach (var param in connectionFields)
                    {
                        Parameters.Add(new ConnectionViewParameter
                        {
                            Name = param.Name
                        });
                    }
                }
                else
                {
                    // SMA
                    throw new NotImplementedException();
                }
            }
        }

        public ObservableCollection<ConnectionViewParameter> Parameters { get; set; }

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
                return string.Empty;
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
                //model.Value = JsonConverter.ToJson(value);
                model.ViewModel = this;

                Owner.Save(this);
                //Owner.Context.AddToVariables(model);

                // Update the UI to notify that the changes has been saved
                UnsavedChanges = false;
                NotifyOfPropertyChange(() => DisplayName);
            });
        }

        public class ConnectionViewParameter
        {
            public string Name { get; set; }

            public object Value { get; set; }
        }
    }
}
