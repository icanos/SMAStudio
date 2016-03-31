using Gemini.Framework.Commands;

namespace SMAStudiovNext.Commands
{
    [CommandDefinition]
    public class ViewEnvironmentExplorerCommandDefinition : CommandDefinition
    {
        public const string CommandName = "View.EnvironmentExplorer";

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
                return "Environment Explorer";
            }
        }

        public override string ToolTip
        {
            get
            {
                return "Environment Explorer";
            }
        }
    }
}
