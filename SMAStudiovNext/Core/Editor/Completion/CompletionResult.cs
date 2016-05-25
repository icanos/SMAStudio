using System.Collections.Generic;
using ICSharpCode.AvalonEdit.CodeCompletion;

namespace SMAStudiovNext.Core.Editor.Completion
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
