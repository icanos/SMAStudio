using ICSharpCode.AvalonEdit.CodeCompletion;
using System;
using System.Collections.Generic;

namespace SMAStudiovNext.Modules.Runbook.Editor.Completion
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
