using Gemini.Framework.Commands;
using SMAStudiovNext.Icons;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SMAStudiovNext.Modules.Shell.Commands
{
    [CommandDefinition]
    public class RefreshCommandDefinition : CommandDefinition
    {
        public const string CommandName = "Environment.Refresh";

        public override string Name
        {
            get { return CommandName; }
        }

        public override string Text
        {
            get { return "_Refresh"; }
        }

        public override string ToolTip
        {
            get { return "Refresh"; }
        }

        public override Uri IconSource
        {
            get { return new Uri("pack://application:,,," + IconsDescription.Refresh); }
        }

        [Export]
        public static CommandKeyboardShortcut KeyGesture = new CommandKeyboardShortcut<RefreshCommandDefinition>(new KeyGesture(Key.F5));
    }
}
