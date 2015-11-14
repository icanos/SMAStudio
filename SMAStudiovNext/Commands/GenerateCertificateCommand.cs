using SMAStudiovNext.Core;
using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using System.Windows.Input;

namespace SMAStudiovNext.Commands
{
    public class GenerateCertificateCommand : ICommand
    {
        private const string CertificateName = "AzureAuto.SMAStudio.local";
        private const string CertificatePassword = "SMAStudio2015";

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
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
