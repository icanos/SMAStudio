using Gemini.Framework.ToolBars;
using SMAStudiovNext.Modules.Runbook.Commands;
using System.ComponentModel.Composition;

namespace SMAStudiovNext.Modules.Runbook
{
    internal static class ToolBarDefinitions
    {
        [Export]
        public static ToolBarItemDefinition TestItem = new CommandToolBarItemDefinition<TestCommandDefinition>(
            Shell.ToolBarDefinitions.ExecutionToolBarGroup, 0, ToolBarItemDisplay.IconAndText);

        [Export]
        public static ToolBarItemDefinition CheckInItem = new CommandToolBarItemDefinition<PublishCommandDefinition>(
            Shell.ToolBarDefinitions.ExecutionToolBarGroup, 1, ToolBarItemDisplay.IconAndText);
    }
}
