using System.Management.Automation.Language;
using ICSharpCode.AvalonEdit.Document;
using SMAStudiovNext.Core.Editor.Parser;

namespace SMAStudiovNext.Core.Editor.Completion
{
    public interface ICompletionProvider
    {
        event CompletionResultDelegate OnCompletionCompleted;

        bool IsRunbook(Token token);

        void Initialize();

        /// <summary>
        /// Responsible of trying to fetch completion data for the given context in the runbook or script.
        /// </summary>
        /// <param name="completionWord">Word that should be completed</param>
        /// <param name="content">Content of the runbook/script</param>
        /// <param name="lineContent">Content of the current line (which the caret is placed in)</param>
        /// <param name="line">Document line</param>
        /// <param name="runbookToken">Token which contains the runbook name if we're completing a runbook, otherwise null.</param>
        /// <param name="position">Position in the document</param>
        /// <param name="triggerChar">Character that started the completion. DEPRECATED, always null.</param>
        /// <param name="triggerTag">Used to keep track of when we're in a completion context.</param>
        void GetCompletionData(string completionWord, string content, string lineContent, DocumentLine line, Token runbookToken, int position, char? triggerChar, long triggerTag);

        void GetParameterCompletionData(Token token, string completionWord, long triggerTag);

        /*IList<ICompletionEntry> Keywords { get; set; }

        IList<ICompletionEntry> Runbooks { get; set; }*/

        LanguageContext Context { get; }
    }
}
