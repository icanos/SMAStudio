using Microsoft.Windows.PowerShell.ScriptAnalyzer.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Modules.WindowRunbook.Editor.Parser
{
    public class AnalysisEventArgs
    {
        public AnalysisEventArgs(IEnumerable<DiagnosticRecord> records)
        {
            Records = records;
        }

        public IEnumerable<DiagnosticRecord> Records { get; set; }
    }
}
