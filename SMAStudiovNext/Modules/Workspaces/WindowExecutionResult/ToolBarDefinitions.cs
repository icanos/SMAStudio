﻿using System.ComponentModel.Composition;
using Gemini.Framework.ToolBars;
using SMAStudiovNext.Commands;

namespace SMAStudiovNext.Modules.WindowExecutionResult
{
    internal static class ToolBarDefinitions
    {
        [Export]
        public static ToolBarItemDefinition RunItem = new CommandToolBarItemDefinition<RunCommandDefinition>(
            Shell.ToolBarDefinitions.ExecutionToolBarGroup, 3, ToolBarItemDisplay.IconAndText);

        [Export]
        public static ToolBarItemDefinition PauseItem = new CommandToolBarItemDefinition<PauseCommandDefinition>(
            Shell.ToolBarDefinitions.ExecutionToolBarGroup, 4, ToolBarItemDisplay.IconAndText);

        [Export]
        public static ToolBarItemDefinition StopItem = new CommandToolBarItemDefinition<StopCommandDefinition>(
            Shell.ToolBarDefinitions.ExecutionToolBarGroup, 5, ToolBarItemDisplay.IconAndText);
    }
}
