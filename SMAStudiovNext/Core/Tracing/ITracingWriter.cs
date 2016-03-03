using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Core.Tracing
{
    public interface ITracingWriter
    {
        void Write(string trace);
    }
}
