using Gemini.Framework.Menus;
using SMAStudiovNext.Modules.Shell.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Modules.Shell
{
    public static class MenuDefinitions
    {
        [Export]
        public static ExcludeMenuItemDefinition GeminiNewItem = new ExcludeMenuItemDefinition(Gemini.Modules.Shell.MenuDefinitions.FileNewMenuItem);

        [Export]
        public static ExcludeMenuItemDefinition GeminiOpenItem = new ExcludeMenuItemDefinition(Gemini.Modules.Shell.MenuDefinitions.FileOpenMenuItem);

        [Export]
        public static ExcludeMenuItemDefinition GeminiSaveAsItem = new ExcludeMenuItemDefinition(Gemini.Modules.Shell.MenuDefinitions.FileSaveAsMenuItem);

        [Export]
        public static ExcludeMenuItemDefinition ToolboxItem = new ExcludeMenuItemDefinition(Gemini.Modules.Toolbox.MenuDefinitions.ViewToolboxMenuItem);

        [Export]
        public static ExcludeMenuItemDefinition HistoryItem = new ExcludeMenuItemDefinition(Gemini.Modules.UndoRedo.MenuDefinitions.ViewHistoryMenuItem);

        [Export]
        public static MenuItemDefinition ViewFullscreenItem = new CommandMenuItemDefinition<ViewFullscreenCommandDefinition>(
            Gemini.Modules.MainMenu.MenuDefinitions.ViewPropertiesMenuGroup, 0);
    }
}
