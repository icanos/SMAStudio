using Microsoft.Windows.PowerShell.ScriptAnalyzer;
using SMAStudiovNext.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Threading.Tasks;
using System.Management.Automation;
using Gemini.Modules.Output;
using Caliburn.Micro;
using Microsoft.Windows.PowerShell.ScriptAnalyzer.Generic;
using System.Management.Automation.Language;

namespace SMAStudiovNext.Modules.WindowRunbook.Editor.Parser
{
    public class AnalyzerService : IOutputWriter
    {
        private static Runspace _runspace;

        public static void Start()
        {
            if (SettingsService.CurrentSettings.EnableCodeAnalysis)
            {
                _runspace = RunspaceFactory.CreateRunspace();
                _runspace.Open();

                //ScriptAnalyzer.Instance.Initialize(_runspace, new AnalyzerService());
                ScriptAnalyzer.Instance.Initialize(runspace: _runspace, outputWriter: new AnalyzerService(), includeDefaultRules: true);
            }
        }

        public static void Stop()
        {
            _runspace.Close();
            _runspace.Dispose();
        }

        public static IEnumerable<DiagnosticRecord> Analyze(ScriptBlockAst scriptBlock, Token[] tokens)
        {
            return ScriptAnalyzer.Instance.AnalyzeSyntaxTree(scriptBlock, tokens, string.Empty);
        }

        private readonly IOutput _output;

        public AnalyzerService()
        {
            _output = IoC.Get<IOutput>();
        }

        public void ThrowTerminatingError(ErrorRecord record)
        {
            
        }

        public void WriteDebug(string message)
        {
            //_output.AppendLine("[DEBUG] " + message);
        }

        public void WriteError(ErrorRecord error)
        {
            //_output.AppendLine("[ERROR] " + error.ToString());
        }

        public void WriteVerbose(string message)
        {
            //_output.AppendLine("[VERBOSE] " + message);
        }

        public void WriteWarning(string message)
        {
            //_output.AppendLine("[WARNING] " + message);
        }
    }
}
