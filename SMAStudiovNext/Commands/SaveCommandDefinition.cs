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
    public class SaveCommandDefinition : CommandDefinition
    {
        public const string CommandName = "Shell.Save";
        public override string Name
        {
            get { return CommandName; }
        }

        public override string Text
        {
            get { return "Save"; }
        }

        public override string ToolTip
        {
            get { return "Save"; }
        }

        public override Uri IconSource
        {
            get { return new Uri("pack://application:,,," + IconsDescription.Save); }
        }

        [Export]
        public static CommandKeyboardShortcut KeyGesture = new CommandKeyboardShortcut<SaveCommandDefinition>(new KeyGesture(Key.S, ModifierKeys.Control));
    }
}
