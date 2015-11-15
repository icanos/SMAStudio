using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Vendor.Azure
{
    public class Credential
    {
        public Guid CredentialID { get; set; }

        public string Name { get; set; }

        public string UserName { get; set; }

        public string RawValue { get; set; }
    }
}
