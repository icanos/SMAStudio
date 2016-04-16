namespace SMAStudiovNext.Modules.WindowRunbook.SyntaxHighlightning
{
    /*public class HighlightingColorizer : DocumentColorizingTransformer
    {
        private readonly LanguageContext _languageContext;
        private readonly IThemeManager _themeManager;

        private TextView _textView;
        private Brush _foregroundBrush;
        private string _foreground;

        public HighlightingColorizer(LanguageContext languageContext)
        {
            _languageContext = languageContext;
            _themeManager = AppContext.Resolve<IThemeManager>();
        }

        protected override void OnAddToTextView(TextView textView)
        {
            base.OnAddToTextView(textView);

            _textView = textView;
        }

        protected override void ColorizeLine(DocumentLine line)
        {
            if (line.IsDeleted)
            {
                return;
            }

            // Language context is null, we haven't had time to initialize yet
            if (_languageContext.Tokens == null)
                return;

            var tokens = _languageContext.Tokens.Where(token => (token.Extent.StartOffset >= line.Offset && token.Extent.EndOffset <= line.EndOffset) || (line.LineNumber >= token.Extent.StartLineNumber && line.LineNumber <= token.Extent.EndLineNumber)).ToList();

            foreach (var token in tokens)
            {
                if (token.Kind == TokenKind.EndOfInput)
                    break;

                var lineStartOffset = token.Extent.StartOffset;// - line.Offset;
                var lineStopOffset = token.Extent.EndOffset;// - line.EndOffset;

                if (token.Extent.StartLineNumber != token.Extent.EndLineNumber)
                {
                    // This is a line that is within a bigger block of code
                    lineStartOffset = line.Offset;
                    lineStopOffset = line.EndOffset;
                }

                if (lineStartOffset < line.Offset || lineStartOffset > line.EndOffset)
                    lineStartOffset = line.Offset;

                if (lineStopOffset < line.Offset || lineStopOffset > line.EndOffset)
                    lineStopOffset = line.EndOffset;

                ChangeLinePart(lineStartOffset, lineStopOffset,
                    visualLineElement => ApplyColorToElement(visualLineElement, token));

                if (token is StringExpandableToken && (token as StringExpandableToken).NestedTokens != null)
                {
                    // Expandable string where we may have nested tokens we need to parse
                    foreach (var nestedToken in (token as StringExpandableToken).NestedTokens)
                    {
                        lineStartOffset = nestedToken.Extent.StartOffset;
                        lineStopOffset = nestedToken.Extent.EndOffset;

                        if (lineStartOffset < line.Offset || lineStartOffset > line.EndOffset)
                            lineStartOffset = line.Offset;

                        if (lineStopOffset < line.Offset || lineStopOffset > line.EndOffset)
                            lineStopOffset = line.EndOffset;

                        ChangeLinePart(lineStopOffset, lineStopOffset,
                            visualLineElement => ApplyColorToElement(visualLineElement, nestedToken));
                    }
                }
            }
        }

        private void ApplyColorToElement(VisualLineElement element, Token token)
        {
            var style = _themeManager.CurrentTheme.GetStyle(token.Kind, token.TokenFlags);

            if (style == null)
            {
                if (_foreground == null)
                {
                    _foreground = _themeManager.CurrentTheme.Foreground;
                    _foregroundBrush = (SolidColorBrush)(new BrushConverter().ConvertFrom(_themeManager.CurrentTheme.Foreground));
                }

                return;
            }
            
            var brush = _themeManager.CurrentTheme.GetBrush(style);
            element.TextRunProperties.SetForegroundBrush(brush);

            var typeface = element.TextRunProperties.Typeface;
            if (style.Bold)
                element.TextRunProperties.SetTypeface(new Typeface(typeface.FontFamily, FontStyles.Normal, FontWeights.Bold, typeface.Stretch));
            else if (style.Italic)
                element.TextRunProperties.SetTypeface(new Typeface(typeface.FontFamily, FontStyles.Italic, FontWeights.Normal, typeface.Stretch));
        }
    }*/
}
