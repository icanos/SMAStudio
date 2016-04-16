using System.Collections.ObjectModel;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Input;
using Caliburn.Micro;
using Gemini.Framework;
using SMAStudiovNext.Core;
using SMAStudiovNext.Services;

namespace SMAStudiovNext.Modules.DialogConnectAzure.ViewModels
{
    public class AzureWindowViewModel : PropertyChangedBase
    {
        private readonly ObservableCollection<CertificateItem> _certificates;

        public AzureWindowViewModel()
        {
            _certificates = new ObservableCollection<CertificateItem>();

            LoadCertificates();
        }

        public void LoadCertificates()
        {
            _certificates.Clear();

            var certificateStore = new X509Store("My");
            certificateStore.Open(OpenFlags.ReadOnly);

            foreach (var cert in certificateStore.Certificates)
            {
                _certificates.Add(new CertificateItem(cert.Subject, cert.Thumbprint));
            }

            NotifyOfPropertyChange(() => Certificates);
        }

        public ObservableCollection<CertificateItem> Certificates
        {
            get { return _certificates; }
        }

        public string SubscriptionId
        {
            get; set;
        }

        public string AutomationAccount
        {
            get; set;
        }

        public CertificateItem SelectedCertificate
        {
            get; set;
        }

        public ICommand GenerateCertificateCommand
        {
            get { return AppContext.Resolve<ICommand>("GenerateCertificateCommand"); }
        }

        public ICommand ConnectCommand
        {
            get {
                return new Commands.RelayCommand(() => {
                    // Only allow executing if both subscription id and automation account has values
                    //if (String.IsNullOrEmpty(SubscriptionId) || String.IsNullOrEmpty(AutomationAccount))
                    //    return false;

                    return true;
                },
                () => {
                    var connection = new BackendConnection();
                    connection.IsAzure = true;
                    connection.Name = AutomationAccount;
                    connection.AzureAutomationAccount = AutomationAccount;
                    connection.AzureSubscriptionId = SubscriptionId;
                    connection.AzureCertificateThumbprint = SelectedCertificate.Thumbprint;

                    SettingsService.CurrentSettings.Connections.Add(connection);

                    var application = IoC.Get<IModule>();
                    ((Startup.Module)application).StartConnection(connection);
                });
            }
        }
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
}
