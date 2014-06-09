using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SMAStudio.Editor.CodeCompletion
{
    public class CompletionSnippet : ICompletionData
    {
        public CompletionSnippet(string text)
        {
            Text = text;
        }

        public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
        {
            TextSegment ts = new TextSegment();
            ts.StartOffset = completionSegment.Offset - ReplaceText.Length;
            ts.EndOffset = completionSegment.EndOffset;

            textArea.Document.Replace(ts, this.Text);
        }

        public object Content
        {
            get { return Text; }
        }

        public object Description
        {
            get { return "Description"; }
        }

        public ImageSource Image
        {
            get { return null; }
        }

        public double Priority
        {
            get { return 0; }
        }

        public string Text
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets set when the completion window is about to be displayed, containing the text that needs to be replaced
        /// </summary>
        public string ReplaceText
        {
            get;
            set;
        }
    }
}
