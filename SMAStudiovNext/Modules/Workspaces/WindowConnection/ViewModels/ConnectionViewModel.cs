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
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using SMAStudiovNext.Commands;
using SMAStudiovNext.Exceptions;

namespace SMAStudiovNext.Modules.WindowConnection.ViewModels
{
    public sealed class ConnectionViewModel : Document, IViewModel, ICommandHandler<SaveCommandDefinition>
    {
        private readonly ConnectionModelProxy model;

        public ConnectionViewModel(ConnectionModelProxy connection)
        {
            model = connection;

            if (connection != null && String.IsNullOrEmpty(connection.Name))
            {
                UnsavedChanges = true;
            }
            
            Owner = connection.Context.Service;
            ConnectionTypes = new ObservableCollection<ConnectionTypeModelProxy>();
            Parameters = new ObservableCollection<ConnectionViewParameter>();

            Task.Run(() =>
            {
                // We need to read the info from our backend again since we don't
                // get any property values when enumerating all connections
                try
                {
                    if (connection.ConnectionType != null)
                        connection = connection.Context.Service.GetConnectionDetails(connection);

                    var result = Owner.GetConnectionTypes();

                    var connectionTypeNameProp = default(PropertyInfo);
                    if (connection.ConnectionType != null)
                        connectionTypeNameProp = connection.ConnectionType.GetType().GetProperty("Name");
                    var connectionTypeName = string.Empty;
                    if (connectionTypeNameProp != null)
                        connectionTypeName = connectionTypeNameProp.GetValue(connection.ConnectionType).ToString();

                    Execute.OnUIThread(() =>
                    {
                        foreach (var type in result)
                        {
                            ConnectionTypes.Add(type);

                            if (type.Name.Equals(connectionTypeName, StringComparison.InvariantCultureIgnoreCase))
                                ConnectionType = type;
                        }

                        if (Owner is AzureService && connection.ConnectionType != null)
                        {
                            var fields = (connection.ConnectionFieldValues as List<Vendor.Azure.ConnectionFieldValue>);
                            if (fields.Count > 0)
                            {
                                Parameters.Clear();

                                foreach (var field in fields)
                                {
                                    var paramName = field.ConnectionFieldName;

                                    if (field.IsEncrypted)
                                        paramName = field.ConnectionFieldName + " (encrypted)";

                                    if (!field.IsOptional)
                                        paramName += "*";

                                    Parameters.Add(new ConnectionViewParameter
                                    {
                                        DisplayName = paramName,
                                        Name = field.ConnectionFieldName,
                                        Value = field.Value
                                    });
                                }
                            }
                        }
                        else if (Owner is SmaService && connection.ConnectionType != null)
                        {
                            var fields = (connection.ConnectionFieldValues as List<SMA.ConnectionFieldValue>);
                            if (fields.Count > 0)
                            {
                                Parameters.Clear();

                                foreach (var field in fields)
                                {
                                    var paramName = field.ConnectionFieldName;

                                    if (field.IsEncrypted)
                                        paramName = field.ConnectionFieldName + " (encrypted)";

                                    if (field.IsOptional != null && field.IsOptional.HasValue && !field.IsOptional.Value)
                                        paramName += "*";

                                    Parameters.Add(new ConnectionViewParameter
                                    {
                                        DisplayName = paramName,
                                        Name = field.ConnectionFieldName,
                                        Value = field.Value
                                    });
                                }
                            }
                        }

                        LongRunningOperation.Stop();
                    });

                }
                catch (ApplicationException ex)
                {
                    LongRunningOperation.Stop();
                    GlobalExceptionHandler.Show(ex);
                }
            });
        }

        public override void CanClose(Action<bool> callback)
        {
            if (UnsavedChanges)
            {
                var result = MessageBox.Show("There are unsaved changes in the connection object, changes will be lost. Do you want to continue?", "Unsaved changes", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                {
                    callback(false);
                    return;
                }
            }

            callback(true);
        }

        #region Properties
        private string _description;
        public string Description
        {
            get { return _description; }
            set
            {
                _description = value;
                UnsavedChanges = true;
            }
        }

        public ObservableCollection<ConnectionTypeModelProxy> ConnectionTypes { get; set; }

        private ConnectionTypeModelProxy _connectionType;
        public ConnectionTypeModelProxy ConnectionType
        {
            get { return _connectionType; }
            set
            {
                var connectionTypeToSet = ConnectionTypes.FirstOrDefault(item => item.Name.Equals(value.Name));

                if (connectionTypeToSet != null)
                    _connectionType = connectionTypeToSet;
                else
                    _connectionType = value;

                Parameters.Clear();

                // Determine if SMA or Azure
                if (Owner is AzureService)
                {
                    // Azure
                    var connectionFields = (IList<Vendor.Azure.ConnectionField>)_connectionType.ConnectionFields;
                    foreach (var param in connectionFields)
                    {
                        var paramName = param.Name;

                        if (param.IsEncrypted)
                            paramName = param.Name + " (encrypted)";

                        if (!param.IsOptional)
                            paramName += "*";

                        Parameters.Add(new ConnectionViewParameter
                        {
                            DisplayName = paramName,
                            Name = param.Name
                        });
                    }
                }
                else
                {
                    // SMA
                    var connectionFields = (IList<SMA.ConnectionField>)_connectionType.ConnectionFields;
                    foreach (var param in connectionFields)
                    {
                        var paramName = param.Name;

                        if (param.IsEncrypted)
                            paramName = param.Name + " (encrypted)";

                        if (!param.IsOptional)
                            paramName += "*";

                        Parameters.Add(new ConnectionViewParameter
                        {
                            DisplayName = paramName,
                            Name = param.Name
                        });
                    }
                }

                UnsavedChanges = true;
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
                UnsavedChanges = true;
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
                LongRunningOperation.Start();

                //model.Value = JsonConverter.ToJson(value);
                model.ViewModel = this;
                model.Description = Description;
                model.ConnectionType = ConnectionType.Model;

                foreach (var param in Parameters)
                {
                    (model.ConnectionFieldValues as List<Vendor.Azure.ConnectionFieldValue>).Add(new Vendor.Azure.ConnectionFieldValue
                    {
                        ConnectionFieldName = param.Name,
                        Value = param.Value
                    });
                }

                try
                {
                    Owner.Save(this, command);
                    Owner.Context.AddToConnections(model);
                }
                catch (ApplicationException ex)
                {
                    GlobalExceptionHandler.Show(ex);
                }

                // Update the UI to notify that the changes has been saved
                UnsavedChanges = false;
                NotifyOfPropertyChange(() => DisplayName);

                LongRunningOperation.Stop();
            });
        }

        public class ConnectionViewParameter
        {
            public string DisplayName { get; set; }

            public string Name { get; set; }

            public string Value { get; set; }
        }
    }
}
