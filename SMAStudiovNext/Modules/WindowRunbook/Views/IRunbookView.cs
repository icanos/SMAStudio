using SMAStudiovNext.Modules.Runbook.Editor;

namespace SMAStudiovNext.Modules.Runbook.Views
{
    public interface IRunbookView
    {
        RunbookEditor TextEditor { get; }

        RunbookEditor PublishedTextEditor { get; }

        TextMarkerService TextMarkerService { get; }
    }
}
