using System.Collections.Generic;
using ICSharpCode.AvalonEdit.CodeCompletion;

namespace SMAStudiovNext.Modules.WindowRunbook.Editor.Completion
{
    public class CompletionResult
    {
        public CompletionResult(IList<ICompletionData> completionData)
        {
            CompletionData = completionData;
        }

        public IList<ICompletionData> CompletionData { get; private set; }
    }
}
