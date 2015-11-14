using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Vendor.Azure
{
    public class Parameter
    {
        public string Type { get; set; }

        public bool IsMandatory { get; set; }

        public int Position { get; set; }

        public string DefaultValue { get; set; }
    }
}
