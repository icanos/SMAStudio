using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Language;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Modules.Runbook.Editor.Parser
{
    public class ParseErrorEventArgs : EventArgs
    {
        public ParseErrorEventArgs(ParseError[] parseErrors)
        {
            Errors = parseErrors;
        }

        public ParseError[] Errors { get; set; }
    }
}
