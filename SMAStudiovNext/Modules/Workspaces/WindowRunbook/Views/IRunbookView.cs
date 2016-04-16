using SMAStudiovNext.Modules.WindowRunbook.Editor;

namespace SMAStudiovNext.Modules.WindowRunbook.Views
{
    public interface IRunbookView
    {
        RunbookEditor TextEditor { get; }

        RunbookEditor PublishedTextEditor { get; }

        TextMarkerService TextMarkerService { get; }
    }
}
