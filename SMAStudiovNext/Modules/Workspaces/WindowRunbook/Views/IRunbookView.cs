using SMAStudiovNext.Modules.WindowRunbook.Editor;
using System.Windows.Controls;

namespace SMAStudiovNext.Modules.WindowRunbook.Views
{
    public interface IRunbookView
    {
        RunbookEditor TextEditor { get; }

        RunbookEditor PublishedTextEditor { get; }

        TextMarkerService TextMarkerService { get; }
    }
}
