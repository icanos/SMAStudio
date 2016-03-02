using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Vendor.Azure
{
    public class Connection
    {
        public Connection()
        {

        }

        public string Name { get; set; }

        public string Description { get; set; }

        public DateTime CreationTime { get; set; }

        public DateTime LastModifiedTime { get; set; }

        public ConnectionType ConnectionType { get; set; }
    }
}
