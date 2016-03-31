using Gemini.Framework.Commands;

namespace SMAStudiovNext.Commands
{
    [CommandDefinition]
    public class AboutCommandDefinition : CommandDefinition
    {
        public const string CommandName = "Help.About";

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
                return "About";
            }
        }

        public override string ToolTip
        {
            get
            {
                return "About";
            }
        }
    }
}
