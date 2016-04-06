using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Modules.WindowRunbook.Editor.Debugging
{
    public class StackFrameDetails
    {
        /// <summary>
        /// Gets the path to the script where the stack frame occurred.
        /// </summary>
        public string ScriptPath { get; private set; }

        /// <summary>
        /// Gets the name of the function where the stack frame occurred.
        /// </summary>
        public string FunctionName { get; private set; }

        /// <summary>
        /// Gets the line number of the script where the stack frame occurred.
        /// </summary>
        public int LineNumber { get; private set; }

        /// <summary>
        /// Gets the column number of the line where the stack frame occurred.
        /// </summary>
        public int ColumnNumber { get; private set; }

        /// <summary>
        /// Gets or sets the VariableContainerDetails that contains the auto variables.
        /// </summary>
        public VariableContainerDetails AutoVariables { get; private set; }

        /// <summary>
        /// Gets or sets the VariableContainerDetails that contains the local variables.
        /// </summary>
        public VariableContainerDetails LocalVariables { get; private set; }

        public static StackFrameDetails Create(
            CallStackFrame callStackFrame,
            VariableContainerDetails autoVariables,
            VariableContainerDetails localVariables)
        {
            return new StackFrameDetails
            {
                ScriptPath = callStackFrame.ScriptName ?? "<No File>",
                FunctionName = callStackFrame.FunctionName,
                LineNumber = callStackFrame.Position.StartLineNumber,
                ColumnNumber = callStackFrame.Position.StartColumnNumber,
                AutoVariables = autoVariables,
                LocalVariables = localVariables
            };
        }
    }
}
