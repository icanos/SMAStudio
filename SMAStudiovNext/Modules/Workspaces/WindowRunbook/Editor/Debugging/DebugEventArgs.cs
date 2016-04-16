using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PowerShell.EditorServices;

namespace SMAStudiovNext.Modules.WindowRunbook.Editor.Debugging
{
    public class DebugEventArgs
    {
        public DebugEventArgs(int lineNumber, StackFrameDetails[] stackFrame)
        {
            LineNumber = lineNumber;
            StackFrames = stackFrame;
        }

        public int LineNumber { get; set; }

        public StackFrameDetails[] StackFrames { get; set; }
    }
}
