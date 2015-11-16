using Gemini.Framework.Commands;
using SMAStudiovNext.Icons;
using System;

namespace SMAStudiovNext.Modules.Runbook.Commands
{
    [CommandDefinition]
    public class EditPublishedCommandDefinition : CommandDefinition
    {
        public const string CommandName = "Runbook.Edit";

        public override string Name
        {
            get { return CommandName; }
        }

        public override string Text
        {
            get { return "Edit"; }
        }

        public override string ToolTip
        {
            get { return "Edit the Runbook"; }
        }

        public override Uri IconSource
        {
            get { return new Uri("pack://application:,,," + IconsDescription.Variable); }
        }
    }
}
