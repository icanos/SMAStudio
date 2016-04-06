using System.ComponentModel.Composition;
using Gemini.Framework.ToolBars;
using SMAStudiovNext.Commands;

namespace SMAStudiovNext.Modules.WindowRunbook
{
    internal static class ToolBarDefinitions
    {
        [Export]
        public static ToolBarItemDefinition TestItem = new CommandToolBarItemDefinition<TestCommandDefinition>(
            Shell.ToolBarDefinitions.ExecutionToolBarGroup, 0, ToolBarItemDisplay.IconAndText);

        [Export]
        public static ToolBarItemDefinition CheckOutItem = new CommandToolBarItemDefinition<EditPublishedCommandDefinition>(
            Shell.ToolBarDefinitions.ExecutionToolBarGroup, 1, ToolBarItemDisplay.IconAndText);

        [Export]
        public static ToolBarItemDefinition CheckInItem = new CommandToolBarItemDefinition<PublishCommandDefinition>(
            Shell.ToolBarDefinitions.ExecutionToolBarGroup, 1, ToolBarItemDisplay.IconAndText);

        [Export]
        public static ToolBarItemDefinition RunItem = new CommandToolBarItemDefinition<DebugCommandDefinition>(
            Shell.ToolBarDefinitions.ExecutionToolBarGroup, 2, ToolBarItemDisplay.IconAndText);
    }
}
