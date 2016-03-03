using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SMAStudiovNext.Core.Tracing
{
    public class TracingAdapter
    {
        private static ITracingWriter _writer;
        private static int _nextInvocationId;
        private static object _lock = new object();

        public static void SetWriter(ITracingWriter writer)
        {
            lock (_lock)
            {
                _writer = writer;
            }
        }

        public static bool IsEnabled
        {
            get; set;
        }

        public static long NextInvocationId
        {
            get
            {
                return Interlocked.Increment(ref _nextInvocationId);
            }
        }

        public static void Enter(string invocationId, object instance, string method, IDictionary<string, object> parameters)
        {
            if (IsEnabled)
            {
                _writer.Write(String.Format("{0}: {1} {2}", invocationId, instance, method));
                foreach (var param in parameters)
                {
                    _writer.Write("\t" + param.Key + " = " + param.Value);
                }
            }
        }

        public static void Exit(string invocationId, object result)
        {
            if (IsEnabled)
            {
                _writer.Write(String.Format("{0}: Result = {1}", invocationId, result));
                _writer.Write("-----------------------------------------------------");
            }
        }
    }
}
