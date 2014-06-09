using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudio.Models
{
    public class ErrorListItem
    {
        public string ErrorId { get; set; }

        public int LineNumber { get; set; }

        public string Description { get; set; }

        public string Runbook { get; set; }
    }
}
