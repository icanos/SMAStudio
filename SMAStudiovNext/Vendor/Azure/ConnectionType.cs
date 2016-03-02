using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Vendor.Azure
{
    public class ConnectionType
    {
        public ConnectionType()
        {
            
        }

        public string Name { get; set; }

        public DateTime CreationTime { get; set; }

        public DateTime LastModifiedTime { get; set; }

        public IList<ConnectionField> ConnectionFields { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
