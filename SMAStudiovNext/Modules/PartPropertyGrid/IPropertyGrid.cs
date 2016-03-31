using Gemini.Framework;

namespace SMAStudiovNext.Modules.PartPropertyGrid
{
    public interface IPropertyGrid : ITool
    {
        object SelectedObject { get; set; }
    }
}
