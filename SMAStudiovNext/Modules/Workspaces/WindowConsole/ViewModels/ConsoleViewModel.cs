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
        private IConsoleView _consoleView;

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

            _consoleView = (IConsoleView)view;
            //_consoleView.Control.StartProcess("powershell.exe", "-noprofile -executionPolicy remotesigned");
            //_consoleView.Control.StartProcess("cmd", "powershell.exe");
        }

        protected override void OnDeactivate(bool close)
        {
            base.OnDeactivate(close);

            //_consoleView.Control.StopProcess();
        }

        public override string DisplayName
        {
            get { return "Console"; }
            set { }
        }
    }
}
