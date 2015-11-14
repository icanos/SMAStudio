using Gemini.Framework.Commands;
using Gemini.Framework.Threading;
using SMAStudiovNext.Modules.Azure.Windows;
using SMAStudiovNext.Services;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace SMAStudiovNext.Commands
{
    [CommandHandler]
    public class ConnectWithAzureCommandHandler : ICommandHandler<ConnectWithAzureCommandDefinition>
    {
        private bool _foundCertificate = false;
        public Task Run(Command command)
        {
            // Open the wizard to connect to Azure
            var dialog = new AzureWindow();
            dialog.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;

            dialog.ShowDialog();

            return TaskUtility.Completed;
        }

        public void Update(Command command)
        {
            if (_foundCertificate)
            {
                command.Enabled = false;
                return;
            }

            var certificateStore = new X509Store("My");
            certificateStore.Open(OpenFlags.ReadOnly);

            foreach (var connection in SettingsService.CurrentSettings.Connections)
            {
                if (!connection.IsAzure)
                    continue;

                foreach (var cert in certificateStore.Certificates)
                {
                    if (cert.Thumbprint.Equals(connection.AzureCertificateThumbprint))
                    {
                        _foundCertificate = true;
                        break;
                    }
                }
            }

            certificateStore.Close();

            if (!_foundCertificate)
                command.Enabled = true;
            else
                command.Enabled = false;
        }
    }
}
