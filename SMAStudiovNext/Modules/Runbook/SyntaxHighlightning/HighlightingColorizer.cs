using ICSharpCode.AvalonEdit.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.AvalonEdit.Document;
using SMAStudio.Language;
using System.Windows.Media;
using SMAStudiovNext.Language;

namespace SMAStudiovNext.Modules.Runbook.SyntaxHighlightning
{
    public class HighlightingColorizer : DocumentColorizingTransformer
    {
        private readonly LanguageParser _parser;
        private TextView _textView;

        public HighlightingColorizer(LanguageContext languageContext)
        {
            //_languageContext = languageContext;
            _parser = new LanguageParser();
        }

        protected override void OnAddToTextView(TextView textView)
        {
            base.OnAddToTextView(textView);

            _textView = textView;
        }

        protected override void ColorizeLine(DocumentLine line)
        {
            //Task.Run(async () =>
            //{
            var textLine = _textView.Document.GetLineByNumber(line.LineNumber);
            var lineStr = _textView.Document.GetText(textLine);
            var result = _parser.Parse(lineStr);//_languageContext.ParseLine(lineStr);

            if (result == null || result.Count == 0)
                return;

            foreach (var item in result)
            {
                if (item.Type == ExpressionType.BlockStart || item.Type == ExpressionType.BlockEnd || item.Type == ExpressionType.ExpressionStart
                    || item.Type == ExpressionType.ExpressionEnd || item.Type == ExpressionType.TypeStart || item.Type == ExpressionType.TypeEnd)
                {
                    continue;
                }

                if (_textView.Document.TextLength < (line.Offset + item.Stop))
                    continue;

                try {
                    ChangeLinePart(line.Offset + item.Start, line.Offset + item.Stop,
                        visualLineElement => ApplyColorToElement(visualLineElement, item, Brushes.Gray));
                }
                catch (Exception) { }
            }
            //});
        }

        private void ApplyColorToElement(VisualLineElement element, LanguageSegment segment, Brush brush)
        {
            if (segment.Type != ExpressionType.Keyword)
                return;

            element.TextRunProperties.SetForegroundBrush(brush);
        }
    }
}
