using SMAStudiovNext.Core;
using SMAStudiovNext.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Collections.ObjectModel;

namespace SMAStudiovNext.Modules.ConnectionManager.Windows
{
    /// <summary>
    /// Interaction logic for ConnectionManagerWindow.xaml
    /// </summary>
    public partial class ConnectionManagerWindow : Window, INotifyPropertyChanged
    {
        public ConnectionManagerWindow()
        {
            InitializeComponent();

            DataContext = this;
            Connections = SettingsService.CurrentSettings.Connections.Where(c => !c.IsAzure).ToObservableCollection();

            Connection = new BackendConnection();
            Connection.SmaImpersonatedLogin = true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void SaveButtonClicked(object sender, RoutedEventArgs e)
        {
            if (!String.IsNullOrEmpty(Connection.CleartextPassword))
                Connection.SmaPassword = DataProtection.Protect(Connection.CleartextPassword);

            Connection.CleartextPassword = string.Empty;

            if (ConnectionIndex == -1)
            {
                SettingsService.CurrentSettings.Connections.Add(Connection);
                ConnectionIndex = SettingsService.CurrentSettings.Connections.Count - 1;
            }
            else
            {
                var conn = SettingsService.CurrentSettings.Connections[ConnectionIndex];
                conn.Name = Connection.Name;
                conn.SmaConnectionUrl = Connection.SmaConnectionUrl;
                conn.SmaDomain = Connection.SmaDomain;
                conn.SmaImpersonatedLogin = Connection.SmaImpersonatedLogin;
                conn.SmaPassword = Connection.SmaPassword;
                conn.SmaUsername = Connection.SmaUsername;
            }

            var settingsService = AppContext.Resolve<ISettingsService>();
            settingsService.Save();

            ConnectionsList.SelectedItem = null;
            NotifyPropertyChanged("IsSelected");
        }

        public void CancelButtonClicked(object sender, RoutedEventArgs e)
        {
            if (ConnectionIndex == -1)
            {
                SettingsService.CurrentSettings.Connections.Remove(Connection);
            }

            Close();
        }

        private void NewConnectionClick(object sender, RoutedEventArgs e)
        {
            Connection = new BackendConnection
            {
                Name = "(untitled)"
            };

            ConnectionIndex = -1;
            NotifyPropertyChanged("Connection");

            Connections.Add(Connection);
            NotifyPropertyChanged("Connections");

            ConnectionsList.SelectedItem = Connection;
            NotifyPropertyChanged("IsSelected");
        }

        private void DeleteConnectionClick(object sender, RoutedEventArgs e)
        {
            if (ConnectionsList.SelectedItem != null && MessageBox.Show("Are you sure you want to remove the connection?", "Question", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                var conn = (BackendConnection)ConnectionsList.SelectedItem;

                SettingsService.CurrentSettings.Connections.Remove(conn);
                Connections.Remove(conn);

                NotifyPropertyChanged("Connections");
                NotifyPropertyChanged("IsSelected");
            }
        }

        private void ConnectionSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Connection = (BackendConnection)ConnectionsList.SelectedItem;
            ConnectionIndex = SettingsService.CurrentSettings.Connections.IndexOf(Connection);

            NotifyPropertyChanged("Connection");
            NotifyPropertyChanged("IsSelected");
        }

        private void ConnectionsListClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            /*if (!Connection.Equals(ConnectionsList.Items[ConnectionIndex]) && MessageBox.Show("Do you want to save your changes?", "Unsaved changes", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                // Save
                var conn = (BackendConnection)ConnectionsList[ConnectionIndex]
            }*/
        }

        public ObservableCollection<BackendConnection> Connections
        {
            get; set;
        }

        public BackendConnection Connection
        {
            get; set;
        }

        public Visibility IsSelected
        {
            get
            {
                if (ConnectionsList.SelectedItem != null)
                    return Visibility.Visible;

                return Visibility.Hidden;
            }
        }

        private int ConnectionIndex
        {
            get; set;
        }
    }
}
