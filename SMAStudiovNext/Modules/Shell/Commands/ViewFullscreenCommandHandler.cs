using Gemini.Framework.Commands;
using Gemini.Framework.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using SMAStudiovNext.Commands;

namespace SMAStudiovNext.Modules.Shell.Commands
{
    [CommandHandler]
    public class ViewFullscreenCommandHandler : ICommandHandler<ViewFullscreenCommandDefinition>
    {
        public Task Run(Command command)
        {
            if (Application.Current.MainWindow == null)
                return TaskUtility.Completed;

            var state = Application.Current.MainWindow.WindowState;// = WindowState.Maximized;

            if (state != WindowState.Maximized)
                Application.Current.MainWindow.WindowState = WindowState.Maximized;
            else
                Application.Current.MainWindow.WindowState = WindowState.Normal;

            return TaskUtility.Completed;
        }

        public void Update(Command command)
        {
            
        }
    }
}
