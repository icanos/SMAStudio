using System.ComponentModel.Composition;
using Gemini.Framework.Menus;
using SMAStudiovNext.Commands;

namespace SMAStudiovNext.Modules.PartEnvironmentExplorer
{
    public static class MenuDefinitions
    {
        [Export]
        public static MenuItemDefinition ViewEnvironmentExplorerMenuItem = new CommandMenuItemDefinition<ViewEnvironmentExplorerCommandDefinition>(
            Gemini.Modules.MainMenu.MenuDefinitions.ViewToolsMenuGroup, 0);
    }
}
