using Caliburn.Micro;
using Gemini.Framework;
using Gemini.Framework.Commands;
using Gemini.Framework.Threading;
using SMAStudiovNext.Core;
using SMAStudiovNext.Modules.Startup;
using SMAStudiovNext.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SMAStudiovNext.Commands;

namespace SMAStudiovNext.Modules.Shell.Commands
{
    [CommandHandler]
    public class RefreshCommandHandler : ICommandHandler<RefreshCommandDefinition>
    {
        public Task Run(Command command)
        {
            var application = IoC.Get<IModule>();
            var contexts = (application as Module).GetContexts();

            foreach (var context in contexts)
            {
                //context.ParseTags();
                context.Tags.Clear();
                context.Start();
            }

            return TaskUtility.Completed;
        }

        public void Update(Command command)
        {
            command.Enabled = true;
        }
    }
}
