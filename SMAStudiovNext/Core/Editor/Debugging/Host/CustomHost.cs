using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management.Automation.Host;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SMAStudiovNext.Core.Editor.Debugging.Host
{
    public class CustomHost : PSHost
    {
        public override void SetShouldExit(int exitCode)
        {
            
        }

        public override void EnterNestedPrompt()
        {
            throw new NotImplementedException();
        }

        public override void ExitNestedPrompt()
        {
            throw new NotImplementedException();
        }

        public override void NotifyBeginApplication()
        {
            
        }

        public override void NotifyEndApplication()
        {
            
        }

        public override string Name { get; } = "Automation Studio Host";
        public override Version Version { get; } = new Version(0, 1);
        public override Guid InstanceId { get; } = Guid.NewGuid();
        public override PSHostUserInterface UI { get; } = new CustomHostUserInterface();
        public override CultureInfo CurrentCulture { get; } = Thread.CurrentThread.CurrentCulture;
        public override CultureInfo CurrentUICulture { get; } = Thread.CurrentThread.CurrentUICulture;
    }
}
