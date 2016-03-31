using System;
using Gemini.Framework.Commands;
using SMAStudiovNext.Icons;

namespace SMAStudiovNext.Commands
{
    [CommandDefinition]
    public class PublishCommandDefinition : CommandDefinition
    {
        public const string CommandName = "Runbook.Publish";

        public override string Name
        {
            get { return CommandName; }
        }

        public override string Text
        {
            get { return "Publish"; }
        }

        public override string ToolTip
        {
            get { return "Publish the Runbook"; }
        }

        public override Uri IconSource
        {
            get { return new Uri("pack://application:,,," + IconsDescription.Publish); }
        }
    }
}
