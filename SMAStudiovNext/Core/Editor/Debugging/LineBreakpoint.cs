using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Core.Editor.Debugging
{
    public class LineBreakpoint
    {
        private readonly int _lineNumber;

        public LineBreakpoint(int lineNumber)
        {
            _lineNumber = lineNumber;
        }

        public int Id { get; set; }

        public int Line => _lineNumber;

        public override bool Equals(object obj)
        {
            return (obj as LineBreakpoint)?.Line == Line;
        }

        public override int GetHashCode()
        {
            return Line.GetHashCode();
        }
    }
}
