using System;
using System.Collections.Generic;
using ICSharpCode.AvalonEdit.CodeCompletion;

namespace SMAStudiovNext.Core.Editor.Completion
{
    public class CompletionEventArgs : EventArgs
    {
        public CompletionEventArgs(IList<ICompletionData> data)
        {
            CompletionMatches = data;
        }

        public IList<ICompletionData> CompletionMatches { get; set; }
    }
}
