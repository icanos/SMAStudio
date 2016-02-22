using System;
using System.Security.Cryptography;

namespace SMAStudiovNext.Core
{
    public class DataProtection
    {
        // Create byte array for additional entropy when using Protect method. 
        static byte[] s_aditionalEntropy = { 9, 8, 7, 6, 5 };

        public static byte[] Protect(String data)
        {
            try
            {
                byte[] bytes = new byte[data.Length * sizeof(char)];
                System.Buffer.BlockCopy(data.ToCharArray(), 0, bytes, 0, bytes.Length);
                // Encrypt the data using DataProtectionScope.CurrentUser. The result can be decrypted 
                //  only by the same current user. 
                return ProtectedData.Protect(bytes, s_aditionalEntropy, DataProtectionScope.CurrentUser);
            }
            catch (CryptographicException)
            {
                return null;
            }
        }

        public static byte[] Unprotect(byte[] data)
        {
            if (data == null)
                return null;

            try
            {
                //Decrypt the data using DataProtectionScope.CurrentUser. 
                return ProtectedData.Unprotect(data, s_aditionalEntropy, DataProtectionScope.CurrentUser);
            }
            catch (CryptographicException)
            {
                return null;
            }
        }
    }
}
