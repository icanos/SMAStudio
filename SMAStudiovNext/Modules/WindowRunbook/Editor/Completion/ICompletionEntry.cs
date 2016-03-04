namespace SMAStudiovNext.Modules.Runbook.Editor.Completion
{
    public interface ICompletionEntry
    {
        string DisplayText { get; }

        string Name { get; }

        string Description { get; }
    }
}
