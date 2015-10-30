using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml.Serialization;

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

        [XmlIgnore]
        public string TriggerWord { get; set; }
        [XmlIgnore]
        public int TriggerWordLength { get; set; }

        public string DisplayText { get; set; }
        public virtual string Description { get; set; }
        public string CompletionText { get; set; }

        public virtual void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
        {
            var text = textArea.Document.Text;
            var caretOffset = textArea.Caret.Offset;
            int startOffset = 0;

            string word = "";

            for (int i = caretOffset - 1; i >= 0; i--)
            {
                var ch = text[i];

                if (ch == ' ' || ch == '\t' || ch == '\n' || ch == '\r' || ch == '(')
                {
                    startOffset = i + 1;
                    break;
                }

                word = text[i] + word;
            }

            var segment = new TextSegment();
            segment.StartOffset = startOffset;
            segment.EndOffset = caretOffset;

            textArea.Document.Replace(segment, CompletionText);
        }

        [XmlIgnore]
        public object Content
        {
            get { return DisplayText; }
        }

        object ICSharpCode.AvalonEdit.CodeCompletion.ICompletionData.Description
        {
            get { return Description; }
        }

        [XmlIgnore]
        public virtual ImageSource Image
        {
            get;
            set;
        }

        [XmlIgnore]
        public double Priority
        {
            get;
            set;
        }

        [XmlIgnore]
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
