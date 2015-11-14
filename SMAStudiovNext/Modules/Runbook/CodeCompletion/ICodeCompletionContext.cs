using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Modules.Runbook.CodeCompletion
{
    public interface ICodeCompletionContext
    {
        ICompletionEntry CurrentKeyword { get; set; }

        IList<ICompletionEntry> GetParameters();
    }
}
