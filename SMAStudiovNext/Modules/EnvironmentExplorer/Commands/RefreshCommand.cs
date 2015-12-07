using Caliburn.Micro;
using Gemini.Framework;
using SMAStudiovNext.Modules.Startup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SMAStudiovNext.Modules.EnvironmentExplorer.Commands
{
    public class RefreshCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            var application = IoC.Get<IModule>();
            var contexts = (application as Module).GetContexts();

            foreach (var context in contexts)
            {
                context.Tags.Clear();
                context.Start();
            }
        }
    }
}
