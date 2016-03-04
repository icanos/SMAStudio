using SMAStudio.Modules.Runbook.Editor.Parser;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SMAStudiovNext.Modules.Runbook.Editor.Completion
{
    public interface ICompletionProvider
    {
        Task<CompletionResult> GetCompletionData(string completionWord, string line, int lineNumber, int position, char? triggerChar);

        IList<ICompletionEntry> Keywords { get; set; }

        IList<ICompletionEntry> Runbooks { get; set; }

        LanguageContext Context { get; }
    }
}
