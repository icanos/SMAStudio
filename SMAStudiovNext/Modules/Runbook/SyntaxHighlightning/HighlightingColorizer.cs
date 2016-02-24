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
using SMAStudiovNext.Themes;
using SMAStudiovNext.Core;
using System.Windows;
using SMAStudiovNext.Modules.Runbook.Controls;

namespace SMAStudiovNext.Modules.Runbook.SyntaxHighlightning
{
    public class HighlightingColorizer : DocumentColorizingTransformer
    {
        private readonly LanguageParser _parser;
        private readonly LanguageContext _languageContext;
        private readonly IThemeManager _themeManager;

        private TextView _textView;
        private Brush _foregroundBrush;
        private string _foreground;

        public HighlightingColorizer(LanguageContext languageContext)
        {
            _languageContext = languageContext;
            _parser = new LanguageParser();
            _themeManager = AppContext.Resolve<IThemeManager>();
        }

        protected override void OnAddToTextView(TextView textView)
        {
            base.OnAddToTextView(textView);

            _textView = textView;
        }

        protected override void ColorizeLine(DocumentLine line)
        {
            //_parser.Clear();
            var parser = new LanguageParser();
            var textLine = _textView.Document.GetLineByNumber(line.LineNumber);
            var lineStr = _textView.Document.GetText(textLine);

            if (lineStr == string.Empty)
                return;
            
            var result = _languageContext.GetLine(_textView.Document.Text, line.Offset, line.EndOffset);

            if (result == null || result.Count == 0)
                return;

            foreach (var item in result)
            {
                if (item.Type == ExpressionType.BlockStart || item.Type == ExpressionType.BlockEnd || item.Type == ExpressionType.ExpressionStart
                    || item.Type == ExpressionType.ExpressionEnd || item.Type == ExpressionType.TypeStart || item.Type == ExpressionType.TypeEnd)
                {
                    continue;
                }

                ChangeLinePart(Math.Max(item.Start, line.Offset), Math.Min(item.Stop, line.EndOffset),
                    visualLineElement => ApplyColorToElement(visualLineElement, item, item.Type));
            }
        }

        private void ApplyColorToElement(VisualLineElement element, LanguageSegment segment, ExpressionType exprType)
        {
            var style = _themeManager.CurrentTheme.GetStyle(exprType);
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
    }
}
