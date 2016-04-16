using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
using Caliburn.Micro;
using Gemini.Modules.Output;
using Microsoft.PowerShell.EditorServices;
using Microsoft.PowerShell.EditorServices.Utility;
using SMAStudiovNext.Modules.WindowRunbook.ViewModels;
using SMAStudiovNext.Utils;

namespace SMAStudiovNext.Modules.WindowRunbook.Editor.Debugging
{
    public class DebuggerService2 : IDisposable
    {
        private readonly IList<LineBreakpoint> _breakpoints;
        private readonly RunbookViewModel _runbookViewModel;

        //private PowerShellContext _powerShell;
        //private DebugService _debugService;
        //private Workspace _workspace;
        private readonly EditorSession _editorSession;

        /// <summary>
        /// Cached script path is used so that we can set breakpoints
        /// when debugging a runbook. This is removed when debugging is stopped.
        /// </summary>
        private string _cachedScriptPath;

        private TaskCompletionSource<DebuggerResumeAction> _debuggerExecutionTask; 

        public event EventHandler<DebugEventArgs> DebuggerStopped;
        public event EventHandler<EventArgs> DebuggerFinished;

        public DebuggerService2(RunbookViewModel runbookViewModel)
        {
            Logger.Initialize(Path.Combine(AppHelper.CachePath, "PowerShellEditorServices.log"), LogLevel.Verbose);

            /*_powerShell = new PowerShellContext();
            _workspace = new Workspace(_powerShell.PowerShellVersion);

            _debugService = new DebugService(_powerShell);
            _debugService.DebuggerStopped += OnDebugStopped;*/
            _editorSession = new EditorSession();
            _editorSession.StartSession();
            _editorSession.DebugService.DebuggerStopped += OnDebugStopped;
            _editorSession.ConsoleService.OutputWritten += OnConsoleOutputWritten;
                
            _runbookViewModel = runbookViewModel;

            _breakpoints = new List<LineBreakpoint>();
        }

        private void OnConsoleOutputWritten(object sender, OutputWrittenEventArgs e)
        {
            var output = IoC.Get<IOutput>();
            output.AppendLine("[" + e.OutputType + "] " + e.OutputText);
        }

        private void OnDebugStopped(object sender, DebuggerStopEventArgs e)
        {
            var lineNumber = e.InvocationInfo.ScriptLineNumber - 2;
            
            // Notify the UI
            DebuggerStopped?.Invoke(this, new DebugEventArgs(lineNumber, _editorSession.DebugService.GetStackFrames()));
        }

        public void AddBreakpoint(int lineNumber)
        {
            if (_cachedScriptPath == null)
                CacheRunbook();

            var lineBreakpoint = new LineBreakpoint(lineNumber);

            // We can only have one breakpoint per line
            if (_breakpoints.Contains(lineBreakpoint))
                return;

            _breakpoints.Add(lineBreakpoint);
        }

        public void RemoveBreakpoint(int lineNumber)
        {
            var lineBreakpoint = new LineBreakpoint(lineNumber);

            if (!_breakpoints.Contains(lineBreakpoint))
                return;

            _breakpoints.Remove(lineBreakpoint);
        }

        public Task Start(List<KeyValuePair<string, object>> inputParameters)
        {
            IsActiveDebugging = true;

            if (_cachedScriptPath == null)
                CacheRunbook();

            var scriptFile = _editorSession.Workspace.GetFile(_cachedScriptPath);
            var breakpointDetails = _breakpoints.Select(breakpoint => BreakpointDetails.Create("", breakpoint.Line + 2)).ToArray();

            // Set the breakpoints
            return _editorSession.DebugService
                .SetLineBreakpoints(scriptFile, breakpointDetails)
                .ContinueWith(
                    (t) =>
                    {
                        // Debug the script
                        _editorSession.PowerShellContext
                            .ExecuteScriptAtPath(_cachedScriptPath)
                            .ContinueWith(
                                (t2) =>
                                {
                                    if (_cachedScriptPath != null)
                                    {
                                        File.Delete(_cachedScriptPath);
                                        _cachedScriptPath = null;
                                    }

                                    IsActiveDebugging = false;
                                    DebuggerFinished?.Invoke(this, new EventArgs());
                                });
                    });
        }

        public void StepOver()
        {
            //ResumeDebugger(DebuggerResumeAction.StepOver);
            _editorSession.DebugService.StepOver();
        }

        public void StepInto()
        {
            //ResumeDebugger(DebuggerResumeAction.StepInto);
            _editorSession.DebugService.StepIn();
        }

        public void Continue()
        {
            ResumeDebugger(DebuggerResumeAction.Continue);
        }

        public void Stop()
        {
            _editorSession.DebugService.Abort();
            IsActiveDebugging = false;

            _debuggerExecutionTask = null;

            DebuggerFinished?.Invoke(this, new EventArgs());
        }

        public StackFrameDetails[] GetStackFrames()
        {
            return _editorSession.DebugService.GetStackFrames();
        }

        private void ResumeDebugger(DebuggerResumeAction action)
        {
            _debuggerExecutionTask?.SetResult(action);
        }

        public bool IsActiveDebugging { get; private set; }

        private void CacheRunbook()
        {
            // Make sure that we can cache the runbook in the correct folder
            if (!Directory.Exists(Path.Combine(AppHelper.CachePath, "scripts")))
                Directory.CreateDirectory(Path.Combine(AppHelper.CachePath, "scripts"));

            var paramBlock = BuildParameterBlock();
            var callString = BuildCallCommand();

            _cachedScriptPath = Path.Combine(AppHelper.CachePath, "scripts", _runbookViewModel.Id + ".ps1");
            File.WriteAllText(_cachedScriptPath, paramBlock + Environment.NewLine + Environment.NewLine + _runbookViewModel.Content + Environment.NewLine + Environment.NewLine + callString);
        }

        /// <summary>
        /// This method is used to build a script param block that is used to trigger
        /// the workflow.
        /// </summary>
        /// <returns></returns>
        private string BuildParameterBlock()
        {
            var parameters = _runbookViewModel.GetParameters(string.Empty);
            var paramBlock = "Param({0})";
            var paramList = parameters.Select(p => p.Text.Replace("-", "$")).ToList();

            return string.Format(paramBlock, string.Join(", ", paramList));
        }

        private string BuildCallCommand()
        {
            return _runbookViewModel.Runbook.RunbookName + " " + BuildScriptArguments();
        }

        private string BuildScriptArguments()
        {
            var parameters = _runbookViewModel.GetParameters(string.Empty);
            var callString = parameters.Aggregate("", (current, p) => current + (p.Text + " " + p.Text.Replace("-", "$") + " "));

            return callString.Trim();
        }

        public void Dispose()
        {
            _editorSession.DebugService.DebuggerStopped -= OnDebugStopped;
            _editorSession.DebugService.Abort();

            _editorSession.Dispose();
        }
    }
}
