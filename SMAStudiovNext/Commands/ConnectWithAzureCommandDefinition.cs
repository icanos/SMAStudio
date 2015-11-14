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
    public class ConnectWithAzureCommandDefinition : CommandDefinition
    {
        public const string CommandName = "Connect.Azure";

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
                return "Connect with Azure";
            }
        }

        public override string ToolTip
        {
            get
            {
                return "Connect with Azure Automation";
            }
        }

        public override Uri IconSource
        {
            get { return new Uri("pack://application:,,," + IconsDescription.Cloud); }
        }
    }
}
