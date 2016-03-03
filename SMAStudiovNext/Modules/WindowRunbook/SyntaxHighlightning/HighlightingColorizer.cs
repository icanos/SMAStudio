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

        private Dictionary<int, List<LanguageSegment>> _lineCache = new Dictionary<int, List<LanguageSegment>>();

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
            var parser = new LanguageParser();
            var textLine = _textView.Document.GetLineByNumber(line.LineNumber);
            var lineStr = _textView.Document.GetText(textLine);

            if (lineStr == string.Empty)
                return;

            if (lineStr.EndsWith(" "))
                _lineCache.Remove(line.LineNumber);

            var currentContext = _languageContext.PredictContext(line.LineNumber, lineStr);
            var result = default(List<LanguageSegment>);

            if (currentContext != ExpressionType.MultilineComment)
            {
                if (_lineCache.ContainsKey(line.LineNumber))
                {
                    result = _lineCache[line.LineNumber];
                    result[result.Count - 1].Stop += 1;
                }
                else
                    result = _languageContext.GetLine(lineStr, line.Offset, line.EndOffset);
            }
            else
            {
                result = new List<LanguageSegment>();
                result.Add(new LanguageSegment { Start = 0, Stop = lineStr.Length, Type = ExpressionType.MultilineComment, Value = lineStr, LineNumber = line.LineNumber });
            }

            if (result == null || result.Count == 0)
                return;

            if (_lineCache.ContainsKey(line.LineNumber))
                _lineCache[line.LineNumber] = result;
            else
                _lineCache.Add(line.LineNumber, result);

            foreach (var item in result)
            {
                if (item.Type == ExpressionType.BlockStart || item.Type == ExpressionType.BlockEnd || item.Type == ExpressionType.ExpressionStart
                    || item.Type == ExpressionType.ExpressionEnd || item.Type == ExpressionType.TypeStart || item.Type == ExpressionType.TypeEnd)
                {
                    continue;
                }

                ChangeLinePart(Math.Max(line.Offset + item.Start, line.Offset), Math.Min(line.Offset + item.Stop, line.EndOffset),
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
