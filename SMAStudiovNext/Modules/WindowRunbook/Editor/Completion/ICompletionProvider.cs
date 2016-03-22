using ICSharpCode.AvalonEdit.Document;
using SMAStudiovNext.Modules.Runbook.Editor.Parser;
using System.Collections.Generic;
using System.Management.Automation.Language;
using System.Threading.Tasks;

namespace SMAStudiovNext.Modules.Runbook.Editor.Completion
{
    public interface ICompletionProvider
    {
        event CompletionResultDelegate OnCompletionCompleted;

        bool IsRunbook(Token token);

        void Initialize();

        void GetCompletionData(string completionWord, string content, string lineContent, DocumentLine line, int position, char? triggerChar, long triggerTag);

        void GetParameterCompletionData(Token token, string completionWord, long triggerTag);

        /*IList<ICompletionEntry> Keywords { get; set; }

        IList<ICompletionEntry> Runbooks { get; set; }*/

        LanguageContext Context { get; }
    }
}
