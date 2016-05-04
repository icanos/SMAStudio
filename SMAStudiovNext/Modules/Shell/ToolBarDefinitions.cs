using Gemini.Framework.ToolBars;
using SMAStudiovNext.Commands;
using SMAStudiovNext.Modules.Shell.Commands;
using System.ComponentModel.Composition;

namespace SMAStudiovNext.Modules.Shell
{
    public static class ToolBarDefinitions
    {
        [Export]
        public static ToolBarDefinition ExecutionToolBar = new ToolBarDefinition(2, "Execution");

        [Export]
        public static ToolBarItemGroupDefinition ExecutionToolBarGroup = new ToolBarItemGroupDefinition(
            ExecutionToolBar, 8);

        [Export]
        public static ToolBarDefinition AzureToolBar = new ToolBarDefinition(3, "Azure");

        [Export]
        public static ToolBarItemGroupDefinition AzureToolBarGroup = new ToolBarItemGroupDefinition(
            AzureToolBar, 9);

        [Export]
        public static ExcludeToolBarItemDefinition ExcludeOpenDefinition = new ExcludeToolBarItemDefinition(
            Gemini.Modules.Shell.ToolBarDefinitions.OpenFileToolBarItem);

        [Export]
        public static ExcludeToolBarItemDefinition ExcludeSaveDefinition = new ExcludeToolBarItemDefinition(
            Gemini.Modules.Shell.ToolBarDefinitions.SaveFileToolBarItem);

        //
        // Items to add
        //
        
        [Export]
        public static ToolBarItemDefinition ConnectionManagerDefinition = new CommandToolBarItemDefinition<NewConnectionCommandDefinition>(
            Gemini.Modules.Shell.ToolBarDefinitions.StandardOpenSaveToolBarGroup, 0);

        [Export]
        public static ToolBarItemDefinition SaveDefinition = new CommandToolBarItemDefinition<SaveCommandDefinition>(
            Gemini.Modules.Shell.ToolBarDefinitions.StandardOpenSaveToolBarGroup, 1);

        [Export]
        public static ToolBarItemDefinition ConnectAzureItem = new CommandToolBarItemDefinition<ConnectWithAzureCommandDefinition>(
            AzureToolBarGroup, 10, ToolBarItemDisplay.IconAndText);
    }
}
