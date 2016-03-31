using System.Windows.Media;
using System.Xml.Serialization;

namespace SMAStudiovNext.Themes
{
    public class StylePart
    {
        private SolidColorBrush _foreground;
        private SolidColorBrush _background;

        public StylePart()
        {
            //Types = new List<TokenFlags>();
        }

        [XmlAttribute]
        public string Expression { get; set; }
        //public List<TokenFlags> Types { get; set; }

        //public List<TokenKind> Kinds { get; set; }

        [XmlText]
        public string Foreground { get; set; }

        [XmlAttribute]
        public string Background { get; set; }

        [XmlAttribute]
        public bool Italic { get; set; }

        [XmlAttribute]
        public bool Bold { get; set; }

        public SolidColorBrush GetForegroundBrush()
        {
            var hexColor = Foreground;

            if (_foreground != null)
                return _foreground;

            var brush = (SolidColorBrush)(new BrushConverter().ConvertFrom(hexColor));
            _foreground = brush;

            return brush;
        }

        public SolidColorBrush GetBackgroundBrush()
        {
            var hexColor = Background;

            if (_background != null)
                return _background;

            var brush = (SolidColorBrush)(new BrushConverter().ConvertFrom(hexColor));
            _background = brush;

            return brush;
        }
    }
}
