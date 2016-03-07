using Gemini.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gemini.Framework.Services;
using SMAStudiovNext.Modules.WindowConsole.Views;
using System.Windows.Media;
using System.Threading;
using System.Windows.Input;

namespace SMAStudiovNext.Modules.WindowConsole.ViewModels
{
    public class ConsoleViewModel : Document
    {
        public ConsoleViewModel()
        {

        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
        }

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);

            var consoleView = (IConsoleView)view;
        }

        public override string DisplayName
        {
            get { return "Console"; }
            set { }
        }
    }
}
