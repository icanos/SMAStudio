using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SMAStudio.Models
{
    public class DocumentReference
    {
        /// <summary>
        /// Source runbook that the reference is called from
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Destination runbook that the reference is calling
        /// </summary>
        public string Destination { get; set; }

        /// <summary>
        /// Line number of which the call is happening
        /// </summary>
        public int LineNumber { get; set; }

        public ICommand GoDefinitionCommand
        {
            get { return Core.Resolve<ICommand>("GoDefinition"); }
        }

        public override string ToString()
        {
            return Destination + " (line " + LineNumber + ")";
        }
    }
}
