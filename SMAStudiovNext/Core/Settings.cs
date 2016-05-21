using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using SMAStudiovNext.Utils;
using System.IO;

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
        /// Path to a location where the user wants to store a copy of all runbooks, locally.
        /// This is also the path to where the auto save feature will store a copy, in case of a crash of
        /// Automation Studio.
        /// </summary>
        public string LocalCopyPath { get; set; }

        /// <summary>
        /// Set to true to store a local copy of the runbooks
        /// </summary>
        public bool EnableLocalCopy { get; set; }

        /// <summary>
        /// Auto saving interval, defaults to 60
        /// </summary>
        public int AutoSaveInterval { get; set; }

        /// <summary>
        /// Set to true to enable code analysis
        /// </summary>
        public bool EnableCodeAnalysis { get; set; }

        /// <summary>
        /// Last open folder in the File Explorer
        /// </summary>
        public string LastFileExplorerPath { get; set; }

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

        public bool IsAzureRM { get; set; }

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

        public string AzureSubscriptionName { get; set; }

        public string AzureAutomationAccount { get; set; }

        public string AzureCertificateThumbprint { get; set; }

        public SecureString Decrypt(byte[] value)
        {
            if (value == null || value.Length == 0)
                return new SecureString();

            byte[] pw = DataProtection.Unprotect(value);
            char[] chars = new char[pw.Length / sizeof(char)];

            Buffer.BlockCopy(pw, 0, chars, 0, pw.Length);
            SecureString secStr = new SecureString();

            foreach (var c in chars)
            {
                secStr.AppendChar(c);
            }

            return secStr;
        }

        public string UnsecureDecrypt(byte[] value)
        {
            if (value == null || value.Length == 0)
                return string.Empty;

            byte[] pw = DataProtection.Unprotect(value);
            char[] chars = new char[pw.Length / sizeof(char)];

            Buffer.BlockCopy(pw, 0, chars, 0, pw.Length);

            return new string(chars);
        }

        public string AzureRMConnectionName { get; set; }

        public string AzureRMServicePrincipalId { get; set; }

        public byte[] AzureRMServicePrincipalKey { get; set; }

        public string AzureRMTenantId { get; set; }

        public string AzureRMGroupName { get; set; }

        [XmlIgnore]
        public string AzureRMLocation { get; set; }

        /// <summary>
        /// Used to store the key while editing the connection, nulled when saved.
        /// </summary>
        [XmlIgnore]
        public string AzureRMServicePrincipalCleartextKey { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
