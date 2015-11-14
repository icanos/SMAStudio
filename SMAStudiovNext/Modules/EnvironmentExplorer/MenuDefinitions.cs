using Gemini.Framework.Menus;
using SMAStudiovNext.Modules.EnvironmentExplorer.Commands;
using System.ComponentModel.Composition;

namespace SMAStudiovNext.Modules.EnvironmentExplorer
{
    public static class MenuDefinitions
    {
        [Export]
        public static MenuItemDefinition ViewEnvironmentExplorerMenuItem = new CommandMenuItemDefinition<ViewEnvironmentExplorerCommandDefinition>(
            Gemini.Modules.MainMenu.MenuDefinitions.ViewToolsMenuGroup, 0);
    }
}
