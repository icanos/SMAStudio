using Gemini.Framework.Commands;
using SMAStudiovNext.Icons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Modules.Runbook.Commands
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
