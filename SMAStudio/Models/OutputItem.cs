using SMAStudio.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudio.Models
{
    public class OutputItem
    {
        public OutputItem()
        {
            Values = new List<OutputNameValue>();
        }

        public Guid JobID { get; set; }

        public Guid RunbookVersionID { get; set; }

        public string StreamTypeName { get; set; }

        public Guid TenantID { get; set; }

        public DateTime StreamTime { get; set; }

        public string StreamText { get; set; }

        public IList<OutputNameValue> Values { get; set; }

        public override string ToString()
        {
            string content = "";

            content += "JobID\t\t\t: " + JobID + "\r\n";
            content += "RunbookVersionID\t: " + RunbookVersionID + "\r\n";
            content += "StreamTypeName\t\t: " + StreamTypeName + "\r\n";
            content += "TenantID\t\t: " + TenantID + "\r\n";
            content += "StreamTime\t\t: " + StreamTime + "\r\n";

            string fixedStreamText = StreamText.Replace("\n", "\r\n\t\t\t  ");
            content += "StreamText\t\t: " + fixedStreamText + "\r\n";

            //content += "NameValues\t\t: ";

            return content;
        }
    }

    public class OutputNameValue
    {
        public string Name { get; set; }

        public string Value { get; set; }
    }
}
