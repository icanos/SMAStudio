using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace SMAStudiovNext.Modules.WindowRunbook.Editor.Renderers
{
    public class DebugMarkerRenderer : IMarkerRenderer
    {
        private readonly IconBarMargin _iconBarMargin;
        private readonly TextView _textView;

        public DebugMarkerRenderer(TextView textView, IconBarMargin iconBarMargin)
        {
            _iconBarMargin = iconBarMargin;
            _textView = textView;
        }

        public void Render(DrawingContext drawingContext, VisualLine line, Size pixelSize)
        {
            var lineMiddle = line.GetTextLineVisualYPosition(line.TextLines[0], VisualYPosition.TextMiddle) -
                                 _textView.VerticalOffset;
            var rect = new Rect(3, PixelSnapHelpers.Round(lineMiddle - 8, pixelSize.Height) + 1, 14, 14);

            drawingContext.DrawRoundedRectangle(Brushes.DarkRed, null, rect, 8, 8);
        }
    }
}
