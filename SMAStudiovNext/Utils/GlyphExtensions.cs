using SMAStudiovNext.Core.Editor.Completion;
using System.Windows;
using System.Windows.Media;

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
