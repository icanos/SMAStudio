using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SMAStudiovNext.Utils
{
    /// <summary>
    /// Courtesy of Microsoft, found at Stackoverflow
    /// http://stackoverflow.com/questions/9343594/how-to-call-asynchronous-method-from-synchronous-method-in-c
    /// </summary>
    internal static class AsyncHelper
    {
        private static readonly TaskFactory _myTaskFactory = new
          TaskFactory(CancellationToken.None,
                      TaskCreationOptions.None,
                      TaskContinuationOptions.None,
                      TaskScheduler.Default);

        public static TResult RunSync<TResult>(Func<Task<TResult>> func)
        {
            return AsyncHelper._myTaskFactory
              .StartNew<Task<TResult>>(func)
              .Unwrap<TResult>()
              .GetAwaiter()
              .GetResult();
        }

        public static void RunSync(Func<Task> func)
        {
            AsyncHelper._myTaskFactory
              .StartNew<Task>(func)
              .Unwrap()
              .GetAwaiter()
              .GetResult();
        }
    }
}
