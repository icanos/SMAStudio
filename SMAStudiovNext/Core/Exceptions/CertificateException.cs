using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Core.Exceptions
{
    public class CertificateException : Exception
    {
        public CertificateException(string message)
            : base (message)
        {

        }
    }
}
