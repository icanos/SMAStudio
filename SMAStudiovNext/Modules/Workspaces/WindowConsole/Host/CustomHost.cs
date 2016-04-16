using SMAStudiovNext.Modules.WindowConsole.Views;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management.Automation.Host;
using System.Text;
using System.Threading.Tasks;
using System.Management.Automation.Runspaces;

namespace SMAStudiovNext.Modules.WindowConsole.Host
{
    internal class CustomHost : PSHost, IHostSupportsInteractiveSession
    {
        private readonly ConsoleView _consoleView;
        private Guid _instanceGuid = Guid.NewGuid();

        /// <summary>
        /// A reference to the runspace used to start an interactive session.
        /// </summary>
        public Runspace _pushedRunspace = null;

        public CustomHost(ConsoleView consoleView)
        {
            _consoleView = consoleView;
        }

        #region PSHost Properties

        public override CultureInfo CurrentCulture
        {
            get
            {
                return System.Threading.Thread.CurrentThread.CurrentCulture;
            }
        }

        public override CultureInfo CurrentUICulture
        {
            get
            {
                return System.Threading.Thread.CurrentThread.CurrentUICulture;
            }
        }

        public override Guid InstanceId
        {
            get
            {
                return _instanceGuid;
            }
        }

        public bool IsRunspacePushed
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override string Name
        {
            get
            {
                return "AutomationStudioPowershellHost";
            }
        }

        public Runspace Runspace
        {
            get
            {
                throw new NotImplementedException();
            }
            internal set { throw new NotImplementedException(); }
        }

        public override PSHostUserInterface UI
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override Version Version
        {
            get
            {
                return new Version(0, 1);
            }
        }

        #endregion

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

        public void PopRunspace()
        {
            Runspace = _pushedRunspace;
            _pushedRunspace = null;
        }

        public void PushRunspace(Runspace runspace)
        {
            _pushedRunspace = runspace;
            Runspace = runspace;
        }

        public override void SetShouldExit(int exitCode)
        {
            // TODO: Notify our console that the process stopped
        }
    }
}
