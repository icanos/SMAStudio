using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Vendor.Azure
{
    public class Job
    {
        public Guid JobID { get; set; }

        public string JobStatus { get; set; }

        public DateTime? StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public DateTime CreationTime { get; set; }

        public DateTime LastModifiedTime { get; set; }

        public short? ErrorCount { get; set; }

        public short? WarningCount { get; set; }
    }
}
