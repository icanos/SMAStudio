using Gemini.Framework.Menus;
using SMAStudiovNext.Commands;
using System.ComponentModel.Composition;

namespace SMAStudiovNext.Modules.Tools.FileExplorer
{
    public static class MenuDefinitions
    {
        [Export]
        public static MenuItemDefinition ViewFileExplorerMenuItem = new CommandMenuItemDefinition<ViewFileExplorerCommandDefinition>(
            Gemini.Modules.MainMenu.MenuDefinitions.ViewToolsMenuGroup, 1);
    }
}
