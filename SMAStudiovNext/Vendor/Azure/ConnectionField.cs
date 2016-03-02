using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Vendor.Azure
{
    public class ConnectionField
    {
        public ConnectionField()
        {
            
        }

        public string Type { get; set; }

        public string Name { get; set; }

        public bool IsEncrypted { get; set; }

        public bool IsOptional { get; set; }
    }
}
