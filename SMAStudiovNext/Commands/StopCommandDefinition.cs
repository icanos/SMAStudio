using System;
using Gemini.Framework.Commands;
using SMAStudiovNext.Icons;

namespace SMAStudiovNext.Commands
{
    [CommandDefinition]
    public class StopCommandDefinition : CommandDefinition
    {
        public const string CommandName = "Runbook.Stop";

        public override string Name
        {
            get { return CommandName; }
        }

        public override string Text
        {
            get { return "Stop"; }
        }

        public override string ToolTip
        {
            get { return "Stop a running runbook"; }
        }

        public override Uri IconSource
        {
            get { return new Uri("pack://application:,,," + IconsDescription.Stop); }
        }
    }
}
