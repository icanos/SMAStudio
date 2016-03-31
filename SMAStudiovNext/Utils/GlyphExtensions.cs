using System.Windows;
using System.Windows.Media;
using SMAStudiovNext.Modules.WindowRunbook.Editor.Completion;

namespace SMAStudiovNext.Utils
{
    public static class GlyphExtensions
    {
        public static ImageSource ToImageSource(this Glyph glyph)
        {
            return Application.Current.TryFindResource(glyph) as ImageSource;
        }
    }
}
