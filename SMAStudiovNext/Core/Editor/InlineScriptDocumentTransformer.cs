using ICSharpCode.AvalonEdit.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.AvalonEdit.Document;
using System.Windows.Media;
using SMAStudiovNext.Core.Editor.Parser;
using System.Windows;
using Caliburn.Micro;

namespace SMAStudiovNext.Core.Editor
{
    public class InlineScriptDocumentTransformer : IBackgroundRenderer
    {
        private readonly LanguageContext _languageContext;
        private readonly TextView _textView;

        private Brush _hightlightBrush;
        private IList<BlockSegment> _segments;

        public InlineScriptDocumentTransformer(TextView textView, LanguageContext languageContext)
        {
            _languageContext = languageContext;
            _textView = textView;

            SetBrush(new SolidColorBrush(Color.FromArgb(0x44, 0xcc, 0xcc, 0xcc)));

            // Detect scroll changes
            _textView.ScrollOffsetChanged += _textView_ScrollOffsetChanged;
            Refresh();
        }

        private void _textView_ScrollOffsetChanged(object sender, EventArgs e)
        {
            _textView.InvalidateLayer(Layer);
        }

        public void SetBrush(Brush brush)
        {
            _hightlightBrush = brush;
            _hightlightBrush.Freeze();

            _textView.InvalidateLayer(Layer);
        }

        /// <summary>
        /// Refresh inline scripts
        /// </summary>
        public void Refresh()
        {
            Task.Run(() =>
            {
                _segments = _languageContext.GetInlineScriptBlocks();
                Execute.OnUIThread(() => _textView.InvalidateLayer(Layer));
            });
        }

        public KnownLayer Layer => KnownLayer.Selection;

        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            if (_segments == null)
                return;

            lock (_segments)
            {
                foreach (var segment in _segments)
                {
                    var startLine = textView.GetVisualLine(segment.StartLineNumber);
                    var endLine = textView.GetVisualLine(segment.EndLineNumber);

                    if (startLine == null || endLine == null)
                        continue;

                    var y = startLine.GetTextLineVisualYPosition(startLine.TextLines[0], VisualYPosition.LineTop) - _textView.ScrollOffset.Y;
                    var yEnd = endLine.GetTextLineVisualYPosition(endLine.TextLines[0], VisualYPosition.LineBottom) - _textView.ScrollOffset.Y;

                    var rect = new System.Windows.Rect(0, y, textView.ActualWidth, yEnd - y);
                    drawingContext.DrawRectangle(_hightlightBrush, null, rect);
                }
            }
        }
    }
}
