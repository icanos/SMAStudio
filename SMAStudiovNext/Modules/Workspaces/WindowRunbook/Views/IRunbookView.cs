using SMAStudiovNext.Core.Editor;

namespace SMAStudiovNext.Modules.WindowRunbook.Views
{
    public interface IRunbookView
    {
        CodeEditor TextEditor { get; }

        CodeEditor PublishedTextEditor { get; }

        TextMarkerService TextMarkerService { get; }
    }
}
