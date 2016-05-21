using Gemini.Framework.Commands;

namespace SMAStudiovNext.Commands
{
    [CommandDefinition]
    public class ViewFileExplorerCommandDefinition : CommandDefinition
    {
        public const string CommandName = "View.FileExplorer";

        public override string Name
        {
            get
            {
                return CommandName;
            }
        }

        public override string Text
        {
            get
            {
                return "File Explorer";
            }
        }

        public override string ToolTip
        {
            get
            {
                return "File Explorer";
            }
        }
    }
}
