using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Core.Editor.Completion
{
    /// <summary>
    /// Thanks to https://github.com/adamdriscoll/poshtools/
    /// </summary>
    internal enum EdgeTrackingMode
    {
        NoneEdgeIncluded,
        LeftEdgeIncluded,
        RightEdgeIncluded,
        BothEdgesIncluded
    }
}
