using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Core.Editor.Parser
{
    public class BlockSegment
    {
        public int StartLineNumber { get; set; }

        public int EndLineNumber { get; set; }

        public int StartOffset { get; set; }

        public int EndOffset { get; set; }
    }
}
