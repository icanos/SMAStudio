using System;
using Gemini.Framework.Commands;
using SMAStudiovNext.Icons;

namespace SMAStudiovNext.Commands
{
    [CommandDefinition]
    public class RunCommandDefinition : CommandDefinition
    {
        public const string CommandName = "Runbook.Run";

        public override string Name
        {
            get { return CommandName; }
        }

        public override string Text
        {
            get { return "Run"; }
        }

        public override string ToolTip
        {
            get { return "Run a published runbook"; }
        }

        public override Uri IconSource
        {
            get { return new Uri("pack://application:,,," + IconsDescription.Run); }
        }
    }
}
