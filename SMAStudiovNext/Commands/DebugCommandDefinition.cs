using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gemini.Framework.Commands;
using SMAStudiovNext.Icons;

namespace SMAStudiovNext.Commands
{
    [CommandDefinition]
    public class DebugCommandDefinition : CommandDefinition
    {
        public const string CommandName = "Runbook.Debug";

        public override string Name
        {
            get { return CommandName; }
        }

        public override string Text
        {
            get { return "Debug"; }
        }

        public override string ToolTip
        {
            get { return "Debug a drafted runbook"; }
        }

        public override Uri IconSource
        {
            get { return new Uri("pack://application:,,," + IconsDescription.Run); }
        }
    }
}
