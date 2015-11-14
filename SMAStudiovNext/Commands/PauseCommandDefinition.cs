using Gemini.Framework.Commands;
using SMAStudiovNext.Icons;
using System;

namespace SMAStudiovNext.Commands
{
    [CommandDefinition]
    public class PauseCommandDefinition : CommandDefinition
    {
        public const string CommandName = "Execution.Pause";

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
                return "Pause";
            }
        }

        public override string ToolTip
        {
            get
            {
                return "Pause Execution";
            }
        }

        public override Uri IconSource
        {
            get { return new Uri("pack://application:,,," + IconsDescription.Pause); }
        }
    }
}
