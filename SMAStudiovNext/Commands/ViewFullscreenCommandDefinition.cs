using Gemini.Framework.Commands;
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
