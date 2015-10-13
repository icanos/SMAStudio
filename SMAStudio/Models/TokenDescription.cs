using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Language;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudio.Models
{
    public class TokenDescription
    {
        public TokenKind Kind { get; set; }

        public TokenFlags Flags { get; set; }
    }
}
