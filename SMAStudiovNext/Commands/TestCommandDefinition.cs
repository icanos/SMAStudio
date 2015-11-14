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
    public class TestCommandDefinition : CommandDefinition
    {
        public const string CommandName = "Runbook.Test";

        public override string Name
        {
            get { return CommandName; }
        }

        public override string Text
        {
            get { return "Test"; }
        }

        public override string ToolTip
        {
            get { return "Test Runbook"; }
        }

        public override Uri IconSource
        {
            get { return new Uri("pack://application:,,," + IconsDescription.Test); }
        }
    }
}
