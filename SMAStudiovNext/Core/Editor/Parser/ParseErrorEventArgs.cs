using System;
using System.Management.Automation.Language;

namespace SMAStudiovNext.Core.Editor.Parser
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
