using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudio.Models
{
    public class DocumentTemplate
    {
        public DocumentTemplate()
        {
            Type = "Runbook Template";
        }

        public string Name { get; set; }

        public string Path { get; set; }

        public string Type { get; set; }

        public string Description { get; set; }
    }
}
