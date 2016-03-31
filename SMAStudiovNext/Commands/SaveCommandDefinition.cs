using System;
using System.ComponentModel.Composition;
using System.Windows.Input;
using Gemini.Framework.Commands;
using SMAStudiovNext.Icons;

namespace SMAStudiovNext.Commands
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
