using System.Management.Automation.Language;
using ICSharpCode.AvalonEdit.Document;
using SMAStudiovNext.Modules.WindowRunbook.Editor.Parser;

namespace SMAStudiovNext.Modules.WindowRunbook.Editor.Completion
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
