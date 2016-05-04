using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using SMAStudiovNext.Core;
using SMAStudiovNext.Services;
using SMAStudiovNext.Utils;
using System;
using SMAStudiovNext.Modules.Dialogs.DialogConnectionManager;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Diagnostics;

namespace SMAStudiovNext.Modules.DialogConnectionManager.Windows
{
    /// <summary>
    /// Interaction logic for ConnectionManagerWindow.xaml
    /// </summary>
    public partial class ConnectionManagerWindow : Window, INotifyPropertyChanged
    {
        private readonly ObservableCollection<CertificateItem> _certificates;

        private const string CertificateName = "AzureAuto.AutomationStudio.local";
        private string CertificatePassword;

        public ConnectionManagerWindow()
        {
            CertificatePassword = Guid.NewGuid().ToString();
            _certificates = new ObservableCollection<CertificateItem>();

            InitializeComponent();

            DataContext = this;
            Connections = SettingsService.CurrentSettings.Connections.ToObservableCollection();/*Where(c => !c.IsAzure).*/

            LoadCertificates();

            Connection = new BackendConnection {SmaImpersonatedLogin = true};
        }

        private void LoadCertificates()
        {
            _certificates.Clear();

            var certificateStore = new X509Store("My");
            certificateStore.Open(OpenFlags.ReadOnly);

            foreach (var cert in certificateStore.Certificates)
            {
                _certificates.Add(new CertificateItem(cert.Subject, cert.Thumbprint));
            }

            NotifyPropertyChanged("Certificates");
            //NotifyOfPropertyChange(() => Certificates);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void SaveButtonClicked(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(Connection.CleartextPassword))
                Connection.SmaPassword = DataProtection.Protect(Connection.CleartextPassword);

            if (!string.IsNullOrEmpty(Connection.AzureRMServicePrincipalCleartextKey))
                Connection.AzureRMServicePrincipalKey = DataProtection.Protect(Connection.AzureRMServicePrincipalCleartextKey);

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

                if (!string.IsNullOrEmpty(Connection.AzureRMServicePrincipalCleartextKey))
                {
                    Connection.AzureRMServicePrincipalCleartextKey = null;
                }

                conn.AzureRMServicePrincipalKey = Connection.AzureRMServicePrincipalKey;
                conn.AzureRMConnectionName = Connection.AzureRMConnectionName;
                conn.AzureRMServicePrincipalId = Connection.AzureRMServicePrincipalId;
                conn.AzureRMTenantId = Connection.AzureRMTenantId;
                conn.IsAzure = Connection.IsAzure;
                conn.IsAzureRM = Connection.IsAzureRM;
                conn.AzureAutomationAccount = Connection.AzureAutomationAccount;
                conn.AzureCertificateThumbprint = Connection.AzureCertificateThumbprint;
                conn.AzureSubscriptionId = Connection.AzureSubscriptionId;
                conn.AzureSubscriptionName = Connection.AzureSubscriptionName;
            }

            var settingsService = AppContext.Resolve<ISettingsService>();
            settingsService.Save();

            ConnectionsList.SelectedItem = null;
            NotifyPropertyChanged("IsSelected");

            Close();
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

        private void OnRefreshClicked(object sender, EventArgs e)
        {
            LoadCertificates();
        }

        private void ConnectionSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Connection = (BackendConnection)ConnectionsList.SelectedItem;
            ConnectionIndex = SettingsService.CurrentSettings.Connections.IndexOf(Connection);

            NotifyPropertyChanged("Connection");
            NotifyPropertyChanged("IsSelected");

            if (Connection != null)
            {
                if (Connection.IsAzure)
                    connectionType.SelectedIndex = 0;
                else if (Connection.IsAzureRM)
                    connectionType.SelectedIndex = 1;
                else
                    connectionType.SelectedIndex = 2;
            }
        }

        private void ConnectionTypeSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = (e.AddedItems[0] as ComboBoxItem);

            if (item.Content.ToString().Equals("Azure Classic"))
            {
                Connection.IsAzure = true;
                Connection.IsAzureRM = false;
            }
            else if (item.Content.ToString().Equals("Azure Resource Manager"))
            {
                Connection.IsAzure = false;
                Connection.IsAzureRM = true;
            }
            else
            {
                Connection.IsAzure = false;
                Connection.IsAzureRM = false;
            }

            contentControl.ContentTemplateSelector = new ConnectionTemplateSelector();
            NotifyPropertyChanged("Connection");
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

        public ObservableCollection<CertificateItem> Certificates
        {
            get { return _certificates; }
        }

        public class CertificateItem
        {
            public CertificateItem(string name, string thumbprint)
            {
                Name = name;
                Thumbprint = thumbprint;
            }

            public string Name { get; set; }

            public string Thumbprint { get; set; }
        }

        private void OnGenerateCertificateClicked(object sender, RoutedEventArgs e)
        {
            var pfx = CertificateManager.GeneratePfx(CertificateName, CertificatePassword);
            var certificate = CertificateManager.GetCertificateForBytes(pfx.GetBytes(), CertificatePassword);

            File.WriteAllBytes(Path.Combine(AppHelper.CachePath, "AzureAutomation.pfx"), pfx.GetBytes());
            File.WriteAllBytes(Path.Combine(AppHelper.CachePath, "AzureAutomation.cer"), certificate);

            var collection = new X509Certificate2Collection();
            collection.Import(Path.Combine(AppHelper.CachePath, "AzureAutomation.pfx"), CertificatePassword, X509KeyStorageFlags.PersistKeySet);

            var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);

            // Store the certificate
            foreach (var cert in collection)
                store.Add(cert);

            store.Close();

            // Delete the certificate that contains the private key - this is already imported into the cert store
            File.Delete(Path.Combine(AppHelper.CachePath, "AzureAutomation.pfx"));

            MessageBox.Show("The certificate has been generated. Please refresh the certificates list.", "Certificate", MessageBoxButton.OK);

            // Open the folder containing the certificate
            Process.Start("explorer.exe", AppHelper.CachePath);
        }
    }
}
