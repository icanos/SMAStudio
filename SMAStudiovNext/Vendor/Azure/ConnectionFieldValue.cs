using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Vendor.Azure
{
    public class ConnectionFieldValue
    {
        public ConnectionFieldValue()
        {
            SMA.ConnectionFieldValue v = new SMA.ConnectionFieldValue();
            
        }

        public Connection Connection { get; set; }

        public string ConnectionFieldName { get; set; }

        public string ConnectionName { get; set; }

        public string ConnectionTypeName { get; set; }

        public string Value { get; set; }

        public string Type { get; set; }

        public bool IsEncrypted { get; set; }

        public bool IsOptional { get; set; }
    }
}
