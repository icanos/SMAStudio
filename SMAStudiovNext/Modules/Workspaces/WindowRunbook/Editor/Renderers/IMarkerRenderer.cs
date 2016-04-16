using ICSharpCode.AvalonEdit.Rendering;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SMAStudiovNext.Modules.WindowRunbook.Editor.Renderers
{
    public interface IMarkerRenderer
    {
        void Render(DrawingContext drawingContext, VisualLine line, Size pixelSize);
    }
}
