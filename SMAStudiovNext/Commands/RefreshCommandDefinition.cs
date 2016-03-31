using System;
using System.ComponentModel.Composition;
using System.Windows.Input;
using Gemini.Framework.Commands;
using SMAStudiovNext.Icons;

namespace SMAStudiovNext.Commands
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
