using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Vendor.Azure
{
    public class Variable
    {
        public Guid VariableID { get; set; }

        public string Name { get; set; }

        public string Value { get; set; }

        public bool IsEncrypted { get; set; }
    }
}
