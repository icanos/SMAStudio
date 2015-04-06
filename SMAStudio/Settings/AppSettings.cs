using SMAStudio.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SMAStudio.Settings
{
    public class AppSettings
    {
        public string SmaWebServiceUrl { get; set; }
        public bool Impersonate { get; set; }
        public String UserName { get; set; }
        public String Domain { get; set; }
        public byte[] Password { get; set; }

        [XmlIgnore]
        public bool IsConfigured
        {
            get
            {
                if (!String.IsNullOrEmpty(SmaWebServiceUrl))
                    return true;

                return false;
            }
        }

        public SecureString GetPassword()
        {
            byte [] pw = DataProtection.Unprotect(Password);
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
