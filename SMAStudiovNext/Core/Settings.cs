using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SMAStudiovNext.Core
{
    public class Settings
    {
        public Settings()
        {
            TrustedCertificates = new List<string>();
            Connections = new List<BackendConnection>();
        }

        public List<BackendConnection> Connections { get; set; }

        public List<string> TrustedCertificates { get; set; }

        //public string AzureSubscriptionId { get; set; }

        //public string AzureAutomationAccount { get; set; }

        //public bool AzureEnabled { get; set; }

        //public string SmaWebserviceUrl { get; set; }

        //public bool ImpersonatedLogin { get; set; }

        //public string Username { get; set; }

        //public string Domain { get; set; }

        //public byte[] Password { get; set; }

        //public string SmaCertificateThumbprint { get; set; }

        //public string AzureCertificateThumbprint { get; set; }
    }

    public class BackendConnection
    {
        public Guid Id { get; set; }

        public bool IsAzure { get; set; }

        public string Name { get; set; }

        public string SmaConnectionUrl { get; set; }

        public string SmaUsername { get; set; }

        public string SmaDomain { get; set; }

        public byte[] SmaPassword { get; set; }

        public bool SmaImpersonatedLogin { get; set; }

        public string AzureSubscriptionId { get; set; }

        public string AzureAutomationAccount { get; set; }

        public string AzureCertificateThumbprint { get; set; }

        public SecureString GetPassword()
        {
            byte[] pw = DataProtection.Unprotect(SmaPassword);
            char[] chars = new char[pw.Length / sizeof(char)];

            System.Buffer.BlockCopy(pw, 0, chars, 0, pw.Length);
            SecureString secStr = new SecureString();

            foreach (var c in chars)
            {
                secStr.AppendChar(c);
            }

            return secStr;
        }
    }
}
