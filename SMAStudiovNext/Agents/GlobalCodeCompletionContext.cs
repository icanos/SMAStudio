using Caliburn.Micro;
using Gemini.Framework;
using SMAStudiovNext.Icons;
using SMAStudiovNext.Models;
using SMAStudiovNext.Modules.Runbook.CodeCompletion;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using System.Threading;

namespace SMAStudiovNext.Agents
{
    public class GlobalCodeCompletionContext : ICodeCompletionContext, IAgent
    {
        private readonly IModule _application;
        private readonly Thread _backgroundThread;
        private readonly object _syncLock = new object();
        private bool _isRunning = true;

        public GlobalCodeCompletionContext()
        {
            _backgroundThread = new Thread(new ThreadStart(StartInternal));
            _application = IoC.Get<IModule>();

            Keywords = new List<ICompletionEntry>();
            Runbooks = new List<ICompletionEntry>();
        }

        public IList<ICompletionEntry> Keywords { get; set; }

        public IList<ICompletionEntry> Runbooks { get; set; }

        public ICompletionEntry CurrentKeyword { get; set; }

        public int ISmaService { get; private set; }

        public void Start()
        {
            // Enumerate standard commands in Powershell
            var assembly = Assembly.GetAssembly(typeof(Microsoft.PowerShell.Commands.GetHelpCommand));
            var commands = assembly.GetTypes().Where(t => t.BaseType == typeof(System.Management.Automation.PSCmdlet)).ToList();

            foreach (var cmd in commands)
            {
                var customAttributes = cmd.CustomAttributes.ToList();

                if (customAttributes.Count < 1)
                    continue;

                var description = customAttributes[0];

                if (description.ConstructorArguments.Count < 2)
                    continue;

                var verb = description.ConstructorArguments[0];
                var noun = description.ConstructorArguments[1];

                Keywords.Add(new KeywordCompletionData(verb.Value + "-" + noun.Value, this, IconsDescription.Cmdlet));
            }

            _backgroundThread.Start();
        }

        private void StartInternal()
        {
            var runbookHash = 0;
            var lastRunbookHash = 0;
            var contexts = ((SMAStudiovNext.Modules.Startup.Module)_application).GetContexts();

            while (_isRunning)
            {
                for (var i = 0; i < contexts.Count; i++)
                {
                    var context = contexts[i];

                    if (context.Runbooks != null)
                    {
                        var runbooks = context.Runbooks;

                        foreach (var runbook in runbooks)
                            runbookHash += ((RunbookModelProxy)runbook.Tag).RunbookName.Length;

                        if (runbookHash.Equals(lastRunbookHash))
                        {
                            Thread.Sleep(5 * 1000);
                            continue;
                        }

                        Runbooks.Clear();
                        foreach (var runbook in runbooks)
                        {
                            Runbooks.Add(new KeywordCompletionData(((RunbookModelProxy)runbook.Tag).RunbookName, this, IconsDescription.Runbook));
                        }
                    }
                }

                lastRunbookHash = runbookHash;
                Thread.Sleep(5 * 1000);
            }
        }

        public void Stop()
        {
            lock (_syncLock)
            {
                _isRunning = false;
                _backgroundThread.Abort();
            }
        }

        public IList<ICompletionEntry> GetParameters()
        {
            if (CurrentKeyword == null)
                return new List<ICompletionEntry>();

            if (((KeywordCompletionData)CurrentKeyword).Parameters.Count > 0)
                return ((KeywordCompletionData)CurrentKeyword).Parameters;

            using (var context = PowerShell.Create())
            {
                context.AddScript("Get-Command " + CurrentKeyword.Name + " | select -expandproperty parameters");
                var paramsFromPs = context.Invoke();

                if (paramsFromPs.Count > 0)
                {
                    var result = (Dictionary<string, ParameterMetadata>)paramsFromPs[0].BaseObject;

                    lock (result)
                    {
                        foreach (var key in result.Keys)
                        {
                            var paramObj = new ParameterCompletionData(result[key].Name, result[key].ParameterType.Name);
                            ((KeywordCompletionData)CurrentKeyword).Parameters.Add(paramObj);
                        }
                    }
                }
            }

            return ((KeywordCompletionData)CurrentKeyword).Parameters;
        }
    }
}
