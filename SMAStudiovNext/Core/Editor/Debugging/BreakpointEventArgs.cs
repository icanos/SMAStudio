using System;

namespace SMAStudiovNext.Core.Editor.Debugging
{
    public class BreakpointEventArgs : EventArgs
    {
        public BreakpointEventArgs(LineBreakpoint lineBreakpoint)
        {
            Breakpoint = lineBreakpoint;
            IsDeleted = false;
        }

        public BreakpointEventArgs(LineBreakpoint lineBreakpoint, bool isDeleted)
        {
            Breakpoint = lineBreakpoint;
            IsDeleted = isDeleted;
        }

        public LineBreakpoint Breakpoint { get; set; }

        public bool IsDeleted { get; set; }
    }
}
