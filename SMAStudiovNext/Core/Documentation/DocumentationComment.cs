using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Core.Documentation
{
    public class DocumentationComment
    {
        public DocumentationComment()
        {
            Parameters = new Dictionary<string, string>();
        }

        public string Synopsis { get; set; }

        public string Description { get; set; }

        public Dictionary<string, string> Parameters { get; set; }

        public string Notes { get; set; }

        public string Author { get; set; }
    }
}
