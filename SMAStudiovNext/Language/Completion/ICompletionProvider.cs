using SMAStudio.Language;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Language.Completion
{
    public interface ICompletionProvider
    {
        Task<CompletionResult> GetCompletionData(string completionWord, string line, int lineNumber, int position, char? triggerChar);

        IList<ICompletionEntry> Keywords { get; set; }

        IList<ICompletionEntry> Runbooks { get; set; }

        LanguageContext Context { get; }
    }
}
