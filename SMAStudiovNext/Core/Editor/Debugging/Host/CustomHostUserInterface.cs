using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Caliburn.Micro;
using Gemini.Modules.Output;

namespace SMAStudiovNext.Core.Editor.Debugging.Host
{
    public class CustomHostUserInterface : PSHostUserInterface
    {
        private readonly IOutput _output;

        public CustomHostUserInterface()
        {
            _output = IoC.Get<IOutput>();
        }

        public override string ReadLine()
        {
            throw new NotImplementedException();
        }

        public override SecureString ReadLineAsSecureString()
        {
            throw new NotImplementedException();
        }

        public override void Write(string value)
        {
            _output.Append(value);
        }

        public override void Write(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value)
        {
            _output.Append(value);
        }

        public override void WriteLine(string value)
        {
            _output.AppendLine(value);
        }

        public override void WriteErrorLine(string value)
        {
            _output.AppendLine("    ");
            _output.AppendLine("[ERROR] " + value);
            _output.AppendLine("    ");
            _output.AppendLine("    ");
        }

        public override void WriteDebugLine(string message)
        {
            _output.AppendLine("[DEBUG] " + message);
        }

        public override void WriteProgress(long sourceId, ProgressRecord record)
        {
            //_output.AppendLine("[PROGRESS] " + record.Activity + " (" + record.PercentComplete + "% completed)");
        }

        public override void WriteVerboseLine(string message)
        {
            _output.AppendLine("[VERBOSE] " + message);
        }

        public override void WriteWarningLine(string message)
        {
            _output.AppendLine("[WARNING] " + message);
        }

        public override Dictionary<string, PSObject> Prompt(string caption, string message, Collection<FieldDescription> descriptions)
        {
            throw new NotImplementedException();
        }

        public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName)
        {
            throw new NotImplementedException();
        }

        public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName,
            PSCredentialTypes allowedCredentialTypes, PSCredentialUIOptions options)
        {
            throw new NotImplementedException();
        }

        public override int PromptForChoice(string caption, string message, Collection<ChoiceDescription> choices, int defaultChoice)
        {
            throw new NotImplementedException();
        }

        public override PSHostRawUserInterface RawUI { get; } = null;
    }
}
