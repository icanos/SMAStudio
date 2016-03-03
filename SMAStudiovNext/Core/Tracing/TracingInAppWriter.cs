using Caliburn.Micro;
using Gemini.Modules.Output;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Core.Tracing
{
    public class TracingInAppWriter : ITracingWriter
    {
        public void Write(string trace)
        {
            var output = IoC.Get<IOutput>();
            output.AppendLine(trace);
        }
    }
}
