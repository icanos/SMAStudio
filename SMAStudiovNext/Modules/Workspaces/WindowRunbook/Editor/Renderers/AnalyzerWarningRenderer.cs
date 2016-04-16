using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Rendering;

namespace SMAStudiovNext.Modules.WindowRunbook.Editor.Renderers
{
    public class AnalyzerWarningRenderer : IMarkerRenderer
    {
        private readonly IconBarMargin _iconBarMargin;
        private readonly TextView _textView;

        public AnalyzerWarningRenderer(TextView textView, IconBarMargin iconBarMargin)
        {
            _iconBarMargin = iconBarMargin;
            _textView = textView;
        }

        public void Render(DrawingContext drawingContext, VisualLine line, Size pixelSize)
        {
            var lineMiddle = line.GetTextLineVisualYPosition(line.TextLines[0], VisualYPosition.TextMiddle) -
                                 _textView.VerticalOffset;

            // Draw the error line
            drawingContext.DrawLine(new Pen(Brushes.DarkGoldenrod, 4), new Point(0, lineMiddle), new Point(20, lineMiddle));
        }
    }
}
