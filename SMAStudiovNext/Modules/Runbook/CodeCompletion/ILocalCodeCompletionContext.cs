using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Modules.Runbook.CodeCompletion
{
    public interface ILocalCodeCompletionContext
    {
        IList<ICompletionEntry> Variables { get; set; }

        IList<ICompletionEntry> Keywords { get; set; }

        IList<ICompletionEntry> GlobalKeywords { get; }

        IList<ICompletionEntry> GlobalRunbooks { get; }

        IList<string> AllModules { get; set; }

        IDictionary<string, IList<string>> UsedModules { get; set; }

        void AddModule(string runbookName, string moduleName);

        void Start();

        void Stop();
    }
}
