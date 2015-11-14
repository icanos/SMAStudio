using SMAStudiovNext.Modules.Runbook.Controls;

namespace SMAStudiovNext.Modules.Runbook.Views
{
    public interface IRunbookView
    {
        RunbookEditor TextEditor { get; }

        RunbookEditor PublishedTextEditor { get; }
    }
}
