using Mono.Security.X509;
using System;
using System.Collections;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using SMAStudiovNext.Services;

namespace SMAStudiovNext.Core
{
    public class CertificateManager
    {
        public static void Configure()
        {
            System.Net.ServicePointManager.ServerCertificateValidationCallback +=
                delegate (object sender, System.Security.Cryptography.X509Certificates.X509Certificate cert, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslError)
                {
                    if (SettingsService.CurrentSettings.TrustedCertificates.Contains(cert.GetCertHashString()))
                        return true;
                    
                    bool chainStatusOk = true;
                    bool containsBaltimoreIssuer = false;
                    foreach (var status in chain.ChainStatus)
                    {
                        if (status.Status != System.Security.Cryptography.X509Certificates.X509ChainStatusFlags.NoError)
                        {
                            chainStatusOk = false;
                            break;
                        }
                    }

                    foreach (var element in chain.ChainElements)
                    {
                        if (element.Certificate.Issuer.Equals("CN=Baltimore CyberTrust Root, OU=CyberTrust, O=Baltimore, C=IE"))
                        {
                            containsBaltimoreIssuer = true;
                            break;
                        }
                    }

                    if (sslError == System.Net.Security.SslPolicyErrors.None && chainStatusOk && containsBaltimoreIssuer)
                        return true;
                    
                    var result = System.Windows.MessageBox.Show("The certificate is invalid, please verify the thumbprint.\r\nThumbprint: " + cert.GetCertHashString() + " - do you want to continue?", "Certificate issues", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
                    
                    if (result == System.Windows.MessageBoxResult.Yes)
                    {
                        SettingsService.CurrentSettings.TrustedCertificates.Add(cert.GetCertHashString());
                        return true;
                    }

                    Console.WriteLine($"Certificate {cert.Subject} ({cert.Issuer}) failed the check.");

                    return false;
                };
        }

        //adapted from https://github.com/mono/mono/blob/master/mcs/tools/security/makecert.cs
        public static PKCS12 GeneratePfx(string certificateName, string password)
        {
            byte[] sn = GenerateSerialNumber();
            string subject = string.Format("CN={0}", certificateName);

            DateTime notBefore = DateTime.Now;
            DateTime notAfter = DateTime.Now.AddYears(20);

            var subjectKey = new RSACryptoServiceProvider(2048);
            var hashName = "SHA512";

            var cb = new X509CertificateBuilder(3);
            cb.SerialNumber = sn;
            cb.IssuerName = subject;
            cb.NotBefore = notBefore;
            cb.NotAfter = notAfter;
            cb.SubjectName = subject;
            cb.SubjectPublicKey = subjectKey;
            cb.Hash = hashName;

            var rawcert = cb.Sign(subjectKey);

            var p12 = new PKCS12();
            p12.Password = password;

            var attributes = GetAttributes();

            p12.AddCertificate(new Mono.Security.X509.X509Certificate(rawcert), attributes);
            p12.AddPkcs8ShroudedKeyBag(subjectKey, attributes);

            return p12;
        }

        public static X509Certificate2 FindCertificate(string certificateThumbprint)
        {
            X509Certificate2 foundCert = null;
            var store = new System.Security.Cryptography.X509Certificates.X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);

            foreach (var cert in store.Certificates)
            {
                if (cert.Thumbprint.Equals(certificateThumbprint))
                {
                    foundCert = cert;
                    break;
                }
            }

            store.Close();

            return foundCert;
        }

        private static Hashtable GetAttributes()
        {
            var list = new ArrayList();

            // we use a fixed array to avoid endianess issues 
            // (in case some tools requires the ID to be 1).
            list.Add(new byte[4] { 1, 0, 0, 0 });
            Hashtable attributes = new Hashtable(1);
            attributes.Add(PKCS9.localKeyId, list);

            return attributes;
        }

        private static byte[] GenerateSerialNumber()
        {
            var sn = Guid.NewGuid().ToByteArray();

            //must be positive
            if ((sn[0] & 0x80) == 0x80)
                sn[0] -= 0x80;
            return sn;
        }

        public static byte[] GetCertificateForBytes(byte[] pfx, string password)
        {
            var pkcs = new PKCS12(pfx, password);
            var cert = pkcs.GetCertificate(GetAttributes());

            return cert.RawData;
        }
    }
}
