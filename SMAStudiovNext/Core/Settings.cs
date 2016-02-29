using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;

namespace SMAStudiovNext.Core
{
    public class Settings
    {
        public Settings()
        {
            TrustedCertificates = new List<string>();
            Connections = new List<BackendConnection>();
        }

        /// <summary>
        /// If debug is set to True, some extended logging is enabled
        /// </summary>
        public bool Debug { get; set; }

        /// <summary>
        /// List of connections to different SMA or Azure Automation accounts
        /// </summary>
        public List<BackendConnection> Connections { get; set; }

        /// <summary>
        /// List of trusted certificates
        /// </summary>
        public List<string> TrustedCertificates { get; set; }

        /// <summary>
        /// Name of the current theme to use
        /// </summary>
        public string Theme { get; set; }
    }

    public class BackendConnection : PropertyChangedBase
    {
        public BackendConnection()
        {
            Id = Guid.NewGuid();
            IsAzure = false;
        }

        public Guid Id { get; set; }

        public bool IsAzure { get; set; }

        private string _name = string.Empty;
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
                NotifyOfPropertyChange(() => Name);
            }
        }

        public string SmaConnectionUrl { get; set; }

        public string SmaUsername { get; set; }

        public string SmaDomain { get; set; }

        public byte[] SmaPassword { get; set; }

        /// <summary>
        /// Used to store the password while editing the connection, nulled when saved.
        /// </summary>
        [XmlIgnore]
        public string CleartextPassword { get; set; }

        private bool _smaImpersonatedLogin = false;
        public bool SmaImpersonatedLogin
        {
            get { return _smaImpersonatedLogin; }
            set
            {
                _smaImpersonatedLogin = value;
                NotifyOfPropertyChange(() => SmaImpersonatedLogin);
            }
        }

        public string AzureSubscriptionId { get; set; }

        public string AzureAutomationAccount { get; set; }

        public string AzureCertificateThumbprint { get; set; }

        public SecureString GetPassword()
        {
            if (SmaPassword == null || SmaPassword.Length == 0)
                return new SecureString();

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

        public override string ToString()
        {
            return Name;
        }
    }
}
