using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;
using Gemini.Modules.Output;
using Microsoft.PowerShell;
using SMAStudiovNext.Modules.WindowRunbook.ViewModels;
using SMAStudiovNext.Utils;
using SMAStudiovNext.Core.Editor.Debugging.Host;

namespace SMAStudiovNext.Core.Editor.Debugging
{
    public class DebuggerService : IDisposable
    {
        private readonly RunbookViewModel _runbookViewModel;
        private readonly IList<LineBreakpoint> _breakpoints;
        private Runspace _runspace;
        private PowerShell _powerShell;
        private InitialSessionState _initialSessionState;
        private CancellationTokenSource _cancellationTokenSource;
        
        /// <summary>
        /// Cached script path is used so that we can set breakpoints
        /// when debugging a runbook. This is removed when debugging is stopped.
        /// </summary>
        private string _cachedScriptPath;
        
        private TaskCompletionSource<DebuggerResumeAction> _debuggerExecutionTask;
        private TaskCompletionSource<IPipelineExecutionRequest> _pipelineExecutionTask;
        private TaskCompletionSource<IPipelineExecutionRequest> _pipelineResultTask;  
        public event EventHandler<DebugEventArgs> DebuggerStopped;
        public event EventHandler<EventArgs> DebuggerFinished;
        //public event EventHandler<BreakpointEventArgs> BreakpointSet;

        private StackFrameDetails[] _stackFrameDetails;
        private List<VariableDetailsBase> _variables;
        private VariableContainerDetails _globalScopeVariables;
        private VariableContainerDetails _scriptScopeVariables;
        private int _pipelineThreadId;
        private IEnumerable<Breakpoint> _setBreakpoints;

        public DebuggerService(RunbookViewModel runbookViewModel)
        {
            _runbookViewModel = runbookViewModel;
            _breakpoints = new List<LineBreakpoint>();
            _variables = new List<VariableDetailsBase>();
            _cancellationTokenSource = new CancellationTokenSource();

            _initialSessionState = InitialSessionState.CreateDefault2();
            _runspace = RunspaceFactory.CreateRunspace(new CustomHost(), _initialSessionState);//(new CustomHost());
            _runspace.ApartmentState = ApartmentState.STA;
            _runspace.ThreadOptions = PSThreadOptions.ReuseThread;
            _runspace.Open();

            _runspace.Debugger.SetDebugMode(DebugModes.LocalScript);
            _runspace.Debugger.DebuggerStop += OnDebuggerStop;

            _powerShell = PowerShell.Create();
            _powerShell.Runspace = _runspace;

            SetExecutionPolicy(_powerShell, ExecutionPolicy.RemoteSigned);
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

        public async Task Start(List<KeyValuePair<string, object>> inputParameters)
        {
            IsActiveDebugging = true;
            
            if (_cachedScriptPath == null)
                CacheRunbook();
            
            // Set the breakpoints
            var command = new PSCommand();

            if (_setBreakpoints != null)
            {
                command.AddCommand(@"Microsoft.PowerShell.Utility\Get-PSBreakpoint");
                command.AddCommand(@"Microsoft.PowerShell.Utility\Remove-PSBreakpoint");

                await ExecuteCommand<object>(command);
                command.Clear();
            }

            if (_breakpoints.Count > 0)
            {
                foreach (var bp in _breakpoints)
                {
                    command.AddCommand("Set-PSBreakpoint")
                        .AddParameter("Script", _cachedScriptPath)
                        .AddParameter("Line", bp.Line + 2);
                }
            }

            if (command.Commands.Count > 0)
            {
                _setBreakpoints = await ExecuteCommand<Breakpoint>(command);
                command.Clear();
            }

            var inputArgs = string.Empty;

            foreach (var input in inputParameters)
            {
                if (input.Key.StartsWith("-"))
                    inputArgs += " " + input.Key;
                else
                    inputArgs += " -" + input.Key;

                if (input.Value is string)
                    inputArgs += " \"" + input.Value + "\"";
                else if (input.Value is bool)
                {
                    if (((bool) input.Value) == true)
                        inputArgs += " $true";
                    else
                        inputArgs += " $false";
                }
                else
                    inputArgs += " " + input.Value;
            }

            command.AddScript(_cachedScriptPath + inputArgs);
            await ExecuteCommand<object>(command, true);
            
            if (_cachedScriptPath != null)
            {
                File.Delete(_cachedScriptPath);
                _cachedScriptPath = null;
            }

            IsActiveDebugging = false;
            DebuggerFinished?.Invoke(this, new EventArgs());

            Stop();
        }
        
        public void StepOver()
        {
            ResumeDebugger(DebuggerResumeAction.StepOver);
        }

        public void StepInto()
        {
            ResumeDebugger(DebuggerResumeAction.StepInto);
        }

        public void Continue()
        {
            ResumeDebugger(DebuggerResumeAction.Continue);
        }

        public void Stop()
        {
            ResumeDebugger(DebuggerResumeAction.Stop);
            IsActiveDebugging = false;

            if (_pipelineResultTask != null)
                _pipelineResultTask.TrySetCanceled();

            if (_pipelineExecutionTask != null)
                _pipelineExecutionTask.TrySetCanceled();

            if (_cancellationTokenSource != null)
                _cancellationTokenSource.Cancel();

            _pipelineExecutionTask = null;
            _pipelineResultTask = null;
            //DebuggerFinished?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Gets the list of stack frames at the point where the
        /// debugger sf stopped.
        /// </summary>
        /// <returns>
        /// An array of StackFrameDetails instances that contain the stack trace.
        /// </returns>
        public StackFrameDetails[] GetStackFrames()
        {
            return _stackFrameDetails;
        }

        /// <summary>
        /// Gets the list of variable scopes for the stack frame that
        /// is identified by the given ID.
        /// </summary>
        /// <param name="stackFrameId">The ID of the stack frame at which variable scopes should be retrieved.</param>
        /// <returns>The list of VariableScope instances which describe the available variable scopes.</returns>
        public VariableScope[] GetVariableScopes(int stackFrameId)
        {
            var localStackFrameVariableId = _stackFrameDetails[stackFrameId].LocalVariables.Id;
            var autoVariablesId = _stackFrameDetails[stackFrameId].AutoVariables.Id;

            return new[]
            {
                new VariableScope(autoVariablesId, VariableContainerDetails.AutoVariablesName),
                new VariableScope(localStackFrameVariableId, VariableContainerDetails.LocalScopeName),
                new VariableScope(_scriptScopeVariables.Id, VariableContainerDetails.ScriptScopeName),
                new VariableScope(_globalScopeVariables.Id, VariableContainerDetails.GlobalScopeName),
            };
        }

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

        private void ResumeDebugger(DebuggerResumeAction action)
        {
            _debuggerExecutionTask?.SetResult(action);
        }

        private async void OnDebuggerStop(object sender, DebuggerStopEventArgs e)
        {
            var lineNumber = e.InvocationInfo.ScriptLineNumber - 2;

            // Create a waitable task so that we can continue when the user has chosen the appropriate action.
            _pipelineThreadId = Thread.CurrentThread.ManagedThreadId;
            _debuggerExecutionTask =
                new TaskCompletionSource<DebuggerResumeAction>();

            // Create a pipeline execution task
            _pipelineExecutionTask = 
                new TaskCompletionSource<IPipelineExecutionRequest>();

            // Get call stack and variables.
            await FetchStackFramesAndVariables();

            // Notify the UI
            DebuggerStopped?.Invoke(this, new DebugEventArgs(lineNumber, GetStackFrames()));
            
            while (true)
            {
                var taskIdx = Task.WaitAny(
                    _debuggerExecutionTask.Task,
                    _pipelineExecutionTask.Task);

                if (taskIdx == 0)
                {
                    // Set the resume action to what the user choose
                    try
                    {
                        e.ResumeAction = _debuggerExecutionTask.Task.Result;
                    }
                    catch (TaskCanceledException) { }
                    catch (AggregateException) { }
                    break;
                }
                else if (taskIdx == 1)
                {
                    try
                    {
                        var executionRequest = _pipelineExecutionTask.Task.Result;

                        _pipelineExecutionTask = new TaskCompletionSource<IPipelineExecutionRequest>();

                        executionRequest.Execute().Wait(_cancellationTokenSource.Token);
                        _pipelineResultTask.SetResult(executionRequest);
                    }
                    catch (TaskCanceledException) { }
                    catch (AggregateException) { }
                }
            }

            _debuggerExecutionTask = null;
        }

        private void SetExecutionPolicy(PowerShell powerShell, ExecutionPolicy desiredExecutionPolicy)
        {
            var currentPolicy = ExecutionPolicy.Undefined;

            // Get the current execution policy so that we don't set it higher than it already is 
            powerShell.Commands.AddCommand("Get-ExecutionPolicy");

            var result = powerShell.Invoke<ExecutionPolicy>();
            if (result.Count > 0)
            {
                currentPolicy = result.FirstOrDefault();
            }

            if (desiredExecutionPolicy < currentPolicy ||
                desiredExecutionPolicy == ExecutionPolicy.Bypass ||
                currentPolicy == ExecutionPolicy.Undefined)
            {
                powerShell.Commands.Clear();
                powerShell
                    .AddCommand("Set-ExecutionPolicy")
                    .AddParameter("ExecutionPolicy", desiredExecutionPolicy)
                    .AddParameter("Scope", ExecutionPolicyScope.Process)
                    .AddParameter("Force");

                powerShell.Invoke();
                powerShell.Commands.Clear();

                // TODO: Ensure there were no errors?
            }
        }

        private async Task FetchStackFramesAndVariables()
        {
            //this.nextVariableId = VariableDetailsBase.FirstVariableId;
            _variables = new List<VariableDetailsBase>();

            // Create a dummy variable for index 0, should never see this.
            _variables.Add(new VariableDetails("Dummy", null));

            // Must retrieve global/script variales before stack frame variables
            // as we check stack frame variables against globals.
            await FetchGlobalAndScriptVariables();
            await FetchStackFrames();
        }

        private async Task FetchGlobalAndScriptVariables()
        {
            // Retrieve globals first as script variable retrieval needs to search globals.
            _globalScopeVariables =
                await FetchVariableContainer(VariableContainerDetails.GlobalScopeName, null);

            _scriptScopeVariables =
                await FetchVariableContainer(VariableContainerDetails.ScriptScopeName, null);
        }

        private async Task FetchStackFrames()
        {
            var psCommand = new PSCommand();
            psCommand.AddScript("return Get-PSCallStack");

            var results = await ExecuteCommand<CallStackFrame>(psCommand);

            var callStackFrames = results.ToArray();
            var stackFrameDetails = new List<StackFrameDetails>();
            //_stackFrameDetails = new StackFrameDetails[callStackFrames.Length];
            
            for (var i = 0; i < callStackFrames.Length; i++)
            {
                var autoVariables =
                    new VariableContainerDetails(
                        VariableContainerDetails.AutoVariablesName);

                _variables.Add(autoVariables);

                try
                {
                    var localVariables =
                        await FetchVariableContainer(i.ToString(), autoVariables);

                    stackFrameDetails.Add(
                        StackFrameDetails.Create(callStackFrames[i], autoVariables, localVariables));
                }
                catch (Exception) { }
            }

            _stackFrameDetails = stackFrameDetails.ToArray();
        }

        private async Task<VariableContainerDetails> FetchVariableContainer(
            string scope,
            VariableContainerDetails autoVariables)
        {
            PSCommand psCommand = new PSCommand();
            psCommand.AddCommand("Get-Variable");
            psCommand.AddParameter("Scope", scope);

            var scopeVariableContainer =
                new VariableContainerDetails("Scope: " + scope);

            _variables.Add(scopeVariableContainer);

            var results = await ExecuteCommand<PSVariable>(psCommand);
            if (results != null)
            {
                foreach (PSVariable psvariable in results)
                {
                    var variableDetails = new VariableDetails(psvariable);// { Id = this.nextVariableId++ };
                    _variables.Add(variableDetails);
                    scopeVariableContainer.Children.Add(variableDetails);//.Add(variableDetails.Name, variableDetails);

                    if ((autoVariables != null) && AddToAutoVariables(psvariable, scope))
                    {
                        autoVariables.Children.Add(variableDetails); //(variableDetails.Name, variableDetails);
                    }
                }
            }

            return scopeVariableContainer;
        }

        private bool AddToAutoVariables(PSVariable psvariable, string scope)
        {
            if ((scope == VariableContainerDetails.GlobalScopeName) ||
                (scope == VariableContainerDetails.ScriptScopeName))
            {
                // We don't A) have a good way of distinguishing built-in from user created variables
                // and B) globalScopeVariables.Children.ContainsKey() doesn't work for built-in variables
                // stored in a child variable container within the globals variable container.
                return false;
            }

            var constantAllScope = ScopedItemOptions.AllScope | ScopedItemOptions.Constant;
            var readonlyAllScope = ScopedItemOptions.AllScope | ScopedItemOptions.ReadOnly;

            // Some local variables, if they exist, should be displayed by default
            if (psvariable.GetType().Name == "LocalVariable")
            {
                if (psvariable.Name.Equals("_"))
                {
                    return true;
                }
                else if (psvariable.Name.Equals("args", StringComparison.OrdinalIgnoreCase))
                {
                    var array = psvariable.Value as Array;
                    return array != null && array.Length > 0;
                }

                return false;
            }
            else if (psvariable.GetType() != typeof(PSVariable))
            {
                return false;
            }

            if (((psvariable.Options | constantAllScope) == constantAllScope) ||
                ((psvariable.Options | readonlyAllScope) == readonlyAllScope))
            {
                var prefixedVariableName = VariableDetails.DollarPrefix + psvariable.Name;
                var node =
                    _globalScopeVariables.Children.FirstOrDefault(
                        x => x.Name.Equals(prefixedVariableName, StringComparison.InvariantCultureIgnoreCase));

                if (node != null)
                {
                    return false;
                }
            }

            if ((psvariable.Value != null) && (psvariable.Value.GetType() == typeof(PSDebugContext)))
            {
                return false;
            }

            return true;
        }

        private async Task<IEnumerable<T>> ExecuteCommand<T>(PSCommand command, bool redirectOutput = false)
        {
            var result = default(IEnumerable<T>);

            if (Thread.CurrentThread.ManagedThreadId != _pipelineThreadId && _pipelineExecutionTask != null)
            {
                // Queue the execution since one task is already running.
                var executionRequest = new PipelineExecutionRequest<T>(this, command, redirectOutput);

                _pipelineResultTask = new TaskCompletionSource<IPipelineExecutionRequest>();

                if (_pipelineExecutionTask.TrySetResult(executionRequest))
                {
                    await _pipelineResultTask.Task;
                    return executionRequest.Results;
                }

                return null;
            }
            
            var scriptOutput = new PSDataCollection<PSObject>();
            if (redirectOutput)
            {
                scriptOutput.DataAdded += (sender, args) =>
                {
                    // Stream script output to console.
                    Execute.OnUIThread(() =>
                    {
                        var output = IoC.Get<IOutput>();

                        foreach (var item in scriptOutput.ReadAll())
                            output.AppendLine(item.ToString());
                    });
                };
            }

            if (_runspace.RunspaceAvailability == RunspaceAvailability.AvailableForNestedCommand
                || _debuggerExecutionTask != null)
            {
                result = ExecuteCommandInDebugger<T>(command, redirectOutput);
            }
            else
            {
                result = await Task.Factory.StartNew<IEnumerable<T>>(() =>
                    {
                        _powerShell.Commands = command;
                        var executionResult = _powerShell.Invoke<T>(scriptOutput);
                        return executionResult;
                    },
                    CancellationToken.None,
                    TaskCreationOptions.None,
                    TaskScheduler.Default
                );
            }

            _powerShell?.Commands.Clear();

            if (_powerShell == null || _powerShell.HadErrors)
                return null;

            return result;
        }

        private IEnumerable<T> ExecuteCommandInDebugger<T>(PSCommand command, bool redirectOutput)
        {
            var outputCollection = new PSDataCollection<PSObject>();

            if (redirectOutput)
            {
                outputCollection.DataAdded += (sender, args) =>
                {
                    // Stream script output to console.
                    Execute.OnUIThread(() =>
                    {
                        var output = IoC.Get<IOutput>();

                        foreach (var item in outputCollection.ReadAll())
                            output.AppendLine(item.ToString());
                    });
                };
            }
            
            _runspace.Debugger.ProcessCommand(
                command, outputCollection);
            
            return
                outputCollection
                    .Select(pso => pso.ImmediateBaseObject)
                    .Cast<T>();
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

        public bool IsActiveDebugging { get; private set; }

        private interface IPipelineExecutionRequest
        {
            Task Execute();
        }

        /// <summary>
        /// Contains details relating to a request to execute a
        /// command on the PowerShell pipeline thread.
        /// </summary>
        /// <typeparam name="TResult">The expected result type of the execution.</typeparam>
        private class PipelineExecutionRequest<TResult> : IPipelineExecutionRequest
        {
            //PowerShellContext powerShellContext;
            private readonly DebuggerService _debuggerService;
            private readonly PSCommand _psCommand;
            private readonly bool _sendOutputToHost;

            public IEnumerable<TResult> Results { get; private set; }

            public PipelineExecutionRequest(
                DebuggerService powerShellContext,
                PSCommand psCommand,
                bool sendOutputToHost)
            {
                _debuggerService = powerShellContext;
                _psCommand = psCommand;
                _sendOutputToHost = sendOutputToHost;
            }

            public async Task Execute()
            {
                Results =
                    await _debuggerService.ExecuteCommand<TResult>(
                        _psCommand,
                        _sendOutputToHost);

                // TODO: Deal with errors?
            }
        }

        public void Dispose()
        {
            try
            {
                _pipelineExecutionTask?.SetCanceled();
                _pipelineResultTask?.SetCanceled();
                _debuggerExecutionTask?.SetCanceled();
            }
            catch (InvalidOperationException) { }

            _cancellationTokenSource.Cancel();

            if (_powerShell != null)
            {
                _powerShell.Dispose();
                _powerShell = null;
            }

            if (_runspace != null)
            {
                _runspace.Close();
                _runspace.Dispose();
                _runspace = null;
            }

            if (_cachedScriptPath != null)
                File.Delete(_cachedScriptPath);
        }
    }
}
