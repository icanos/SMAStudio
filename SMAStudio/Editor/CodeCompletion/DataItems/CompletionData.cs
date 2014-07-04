using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SMAStudio.Editor.CodeCompletion.DataItems
{
    public class CompletionData : ICompletionData
    {
        protected CompletionData()
        {
            Priority = 1;
        }

        public CompletionData(string text)
        {
            DisplayText = CompletionText = Description = text;
            Priority = 1;
        }

        public string TriggerWord { get; set; }
        public int TriggerWordLength { get; set; }

        public string DisplayText { get; set; }
        public virtual string Description { get; set; }
        public string CompletionText { get; set; }

        public virtual void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
        {
            textArea.Document.Replace(completionSegment, CompletionText);
        }

        public object Content
        {
            get { return DisplayText; }
        }

        object ICSharpCode.AvalonEdit.CodeCompletion.ICompletionData.Description
        {
            get { return Description; }
        }

        public virtual ImageSource Image
        {
            get;
            set;
        }

        public double Priority
        {
            get;
            set;
        }

        public string Text
        {
            get { return CompletionText; }
        }

        public override string ToString()
        {
            return DisplayText;
        }

        public override bool Equals(object obj)
        {
            var other = obj as CompletionData;
            return other != null && DisplayText == other.DisplayText;
        }

        public override int GetHashCode()
        {
            return DisplayText.GetHashCode();
        }
    }
}
