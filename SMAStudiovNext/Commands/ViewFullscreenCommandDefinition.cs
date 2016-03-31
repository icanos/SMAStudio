using System.ComponentModel.Composition;
using System.Windows.Input;
using Gemini.Framework.Commands;

namespace SMAStudiovNext.Commands
{
    [CommandDefinition]
    public class ViewFullscreenCommandDefinition : CommandDefinition
    {
        public const string CommandName = "View.FullScreen";

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
                return "Full Screen";
            }
        }

        public override string ToolTip
        {
            get
            {
                return "Full Screen";
            }
        }

        [Export]
        public static CommandKeyboardShortcut KeyGesture = new CommandKeyboardShortcut<ViewFullscreenCommandDefinition>(new KeyGesture(Key.Enter, ModifierKeys.Shift | ModifierKeys.Alt));
    }
}
