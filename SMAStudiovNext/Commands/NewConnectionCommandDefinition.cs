using Gemini.Framework.Commands;
using SMAStudiovNext.Icons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Commands
{
    [CommandDefinition]
    public class NewConnectionCommandDefinition : CommandDefinition
    {
        public const string CommandName = "Shell.NewConnection";

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
                return "New Connection...";
            }
        }

        public override string ToolTip
        {
            get
            {
                return "New Connection to Azure or SMA";
            }
        }

        public override Uri IconSource
        {
            get { return new Uri("pack://application:,,," + IconsDescription.Connection); }
        }
    }
}
