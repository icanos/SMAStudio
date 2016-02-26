using Gemini.Framework.Commands;
using Gemini.Framework.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SMAStudiovNext.Modules.Shell.Commands
{
    [CommandHandler]
    public class AboutCommandHandler : ICommandHandler<AboutCommandDefinition>
    {
        public Task Run(Command command)
        {
            MessageBox.Show("Automation Studio is written by Marcus Westin (marcus@thewestins.nu).\r\nMore info at github.com/icanos/SMAStudio.", "About Automation Studio");

            return TaskUtility.Completed;
        }

        public void Update(Command command)
        {
            
        }
    }
}
