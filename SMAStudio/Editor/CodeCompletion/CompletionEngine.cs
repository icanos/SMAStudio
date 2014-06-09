using ICSharpCode.AvalonEdit.CodeCompletion;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SMAStudio.Editor.CodeCompletion
{
    /// <summary>
    /// Engine that collects all completion data and presents it to the user
    /// </summary>
    public class CompletionEngine
    {
        private const string COLLECT_COMMANDS = "Get-Command -Module Microsoft.Powershell* | ft Name | Out-String";
        private const string COLLECT_MODULES = "Get-Module -ListAvailable | ft Name | Out-String";
        private const string COLLECT_PARAMETERS = "$a = Get-Command {0}; $a.ParameterSets[0] | Select -ExpandProperty parameters | ft Name | Out-String";

        private List<string> _approvedVerbs = new List<string>
            { "Add", "Clear", "Close", "Copy", "Enter", "Exit", "Find", "Format", "Get", "Hide", "Join", "Lock", "Move",
            "New", "Open", "Pop", "Push", "Redo","Remove","Rename","Reset","Search","Select","Set","Show","Skip","Split",
            "Step", "Switch","Undo","Unlock","Watch","Connect","Disconnect","Read","Receive","Send","Write","Backup",
            "Checkpoint","Compare","Compress","Convert","ConvertFrom","ConvertTo", "Dismount", "Edit","Expand","Export","Group",
            "Import", "Initialize","Limit","Merge","Mount","Out","Publish","Restore","Save","Sync","Unpublish","Update",
            "Debug","Measure","Ping","Repair","Resolve","Test","Trace","Approve","Assert","Complete","Confirm","Deny",
            "Disabled","Enable","Install","Invoke","Register","Request","Restart","Resume","Start","Stop","Submit",
            "Suspend","Uninstall","Unregister","Wait","Block","Grant","Protect","Revoke","Unblock","Unprotect","Use"
            };

        private List<ICompletionData> _commands;
        private Dictionary<string, List<string>> _parameters;

        private Runspace _paramRunspace = null;
        private PowerShell _paramPowershell = null;

        public CompletionEngine()
        {
            _commands = new List<ICompletionData>();
            _parameters = new Dictionary<string, List<string>>();
        }

        public void Start()
        {
            Thread thread = new Thread(new ThreadStart(Collect));
            thread.Start();
        }

        /// <summary>
        /// Retrieves all commands and modules available from powershell
        /// This session is executed in a different thread (background thread),
        /// to allow the user to continue work while this is collected.
        /// </summary>
        private void Collect()
        {
            // Retrieve all modules
            InternalCollect(COLLECT_COMMANDS);

            // Retrieve all modules
            InternalCollect(COLLECT_MODULES);

            // Retrieve parameters for all commands
            /*foreach (var cmd in _commands)
            {
                InternalParameterRetrieval(cmd.Text);
            }

            _paramRunspace.Close();
            _paramRunspace = null;*/
        }

        private void InternalCollect(string scriptContent)
        {
            Runspace runspace = RunspaceFactory.CreateRunspace();
            runspace.Open();

            PowerShell powershell = PowerShell.Create();
            powershell.Runspace = runspace;

            // Retrieve all commands
            powershell.AddScript(scriptContent);
            Collection<PSObject> commands = powershell.Invoke();

            var cmd = commands[0];
            var cmds = cmd.ToString().Split('\n');

            for (int i = 3; i < cmds.Length; i++)
            {
                var snippet = new CompletionSnippet(cmds[i].Trim());

                if (!Commands.Contains(snippet))
                    Commands.Add(snippet);
            }

            runspace.Close();
        }

        private void InternalParameterRetrieval(string command)
        {
            if (_paramRunspace == null)
            {
                _paramRunspace = RunspaceFactory.CreateRunspace();
                _paramRunspace.Open();

                _paramPowershell = PowerShell.Create();
                _paramPowershell.Runspace = _paramRunspace;
            }

            _paramPowershell.Commands.Clear();

            _paramPowershell.AddScript(String.Format(COLLECT_PARAMETERS, command));
            Collection<PSObject> parameters = _paramPowershell.Invoke();

            if (parameters.Count == 0)
                return;

            var cmd = parameters[0];
            var cmds = cmd.ToString().Split('\n');

            for (int i = 3; i < cmds.Length; i++)
            {
                if (_parameters.ContainsKey(command))
                    _parameters[command].Add(cmds[i].Trim());
                else
                {
                    _parameters.Add(command, new List<string>());
                    _parameters[command].Add(cmds[i].Trim());
                }
            }
        }

        public List<ICompletionData> Commands
        {
            get { return _commands; }
            set { _commands = value; }
        }

        public Dictionary<string, List<string>> Parameters
        {
            get { return _parameters; }
            set { _parameters = value; }
        }

        public List<string> ApprovedVerbs
        {
            get { return _approvedVerbs; }
            set { _approvedVerbs = value; }
        }
    }
}
