using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Language.Completion
{
    public interface ICompletionEntry
    {
        string DisplayText { get; }

        string Name { get; }

        string Description { get; }
    }
}
