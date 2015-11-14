using System;

namespace SMAStudiovNext.Core
{
    public class JobOutput
    {
        public Guid JobID { get; set; }

        public Guid RunbookVersionID { get; set; }

        public string StreamTypeName { get; set; }

        public Guid TenantID { get; set; }

        public DateTime StreamTime { get; set; }

        public string StreamText { get; set; }
    }
}
