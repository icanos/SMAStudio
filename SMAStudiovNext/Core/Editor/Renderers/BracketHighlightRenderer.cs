using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using SMAStudiovNext.Core.Editor.Parser;

namespace SMAStudiovNext.Core.Editor.Renderers
{
    public class BracketHighlightRenderer : IBackgroundRenderer
    {
        private static readonly Color DefaultBackground = Color.FromArgb(22, 0, 0, 255);
        private static readonly Color DefaultBorder = Color.FromArgb(52, 0, 0, 255);

        private readonly TextView _textView;
        private readonly LanguageContext _languageContext;

        private Pen _borderPen;
        private Brush _backgroundBrush;
        private BracketSearchResult _result;

        public BracketHighlightRenderer(TextView textView, LanguageContext languageContext)
        {
            _languageContext = languageContext;
            _textView = textView;

            UpdateColors(DefaultBackground, DefaultBorder);
        }

        public void SetHighlight(BracketSearchResult result)
        {
            if (_result != result)
            {
                _result = result;
                _textView.InvalidateLayer(Layer);
            }
        }

        public void UpdateColors(Color background, Color foreground)
        {
            _borderPen = new Pen(new SolidColorBrush(foreground), 1);
            _borderPen.Freeze();

            _backgroundBrush = new SolidColorBrush(background);
            _backgroundBrush.Freeze();
        }

        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            if (_result == null)
                return;

            var builder = new BackgroundGeometryBuilder();

            builder.CornerRadius = 1;
            builder.AlignToMiddleOfPixels = true;

            builder.AddSegment(textView, new TextSegment() { StartOffset = _result.OpeningBracketOffset, Length = _result.OpeningBracketLength });
            builder.CloseFigure();
            builder.AddSegment(textView, new TextSegment() { StartOffset = _result.ClosingBracketOffset, Length = _result.ClosingBracketLength });

            var geometry = builder.CreateGeometry();
            if (geometry != null)
            {
                drawingContext.DrawGeometry(_backgroundBrush, _borderPen, geometry);
            }
        }

        public KnownLayer Layer => KnownLayer.Selection;
    }
}
