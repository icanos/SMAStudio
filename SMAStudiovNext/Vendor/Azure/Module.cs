using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Vendor.Azure
{
    public class Module
    {
        public string ModuleName { get; set; }

        public string ModuleUrl { get; set; }

        public string ModuleVersion { get; set; }

        public DateTime CreationTime { get; set; }

        public DateTime LastModifiedTime { get; set; }
    }
}
