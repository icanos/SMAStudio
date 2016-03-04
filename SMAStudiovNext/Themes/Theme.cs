using SMAStudio.Modules.Runbook.Editor.Parser;
using SMAStudiovNext.Language;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SMAStudiovNext.Themes
{
    public class Theme
    {
        private Dictionary<string, SolidColorBrush> _brushCache = null;

        public Theme()
        {
            _brushCache = new Dictionary<string, SolidColorBrush>();
        }

        public string Name { get; set; }

        public string Font { get; set; }

        public double FontSize { get; set; }

        public string Background { get; set; }

        public string Foreground { get; set; }

        public List<StylePart> Colors { get; set; }

        public StylePart GetStyle(ExpressionType exprType)
        {
            var style = Colors.FirstOrDefault(item => item.Expression == exprType);

            return style;
        }

        public SolidColorBrush GetBrush(StylePart style)
        {
            if (style == null)
                return Brushes.Black;

            var hexColor = style.Color;

            if (_brushCache.ContainsKey(hexColor))
                return _brushCache[hexColor];

            var brush = (SolidColorBrush)(new BrushConverter().ConvertFrom(hexColor));
            _brushCache.Add(hexColor, brush);

            return brush;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
