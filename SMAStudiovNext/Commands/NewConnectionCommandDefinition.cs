using Gemini.Framework.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Commands
{
    public class NewConnectionCommandDefinition : CommandDefinition
    {
        public const string CommandName = "File.NewConnection";

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
    }
}
