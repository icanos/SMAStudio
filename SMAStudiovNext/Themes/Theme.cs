using System.Collections.Generic;
using System.Windows.Media;

namespace SMAStudiovNext.Themes
{
    public class Theme
    {
        private readonly Dictionary<string, SolidColorBrush> _brushCache;

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

        public SolidColorBrush GetBrush(StylePart style)
        {
            if (style == null)
                return Brushes.Black;

            var hexColor = style.Foreground;

            if (_brushCache.ContainsKey(hexColor))
                return _brushCache[hexColor];

            var brush = (SolidColorBrush) new BrushConverter().ConvertFrom(hexColor);
            _brushCache.Add(hexColor, brush);

            return brush;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}