using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SMAStudio.Services
{
    sealed class AsyncService
    {
        private static IList<Thread> _runningThreads = new List<Thread>();

        public AsyncService()
        {

        }

        /// <summary>
        /// Executes a chunk of code in a separate thread
        /// </summary>
        /// <param name="priority">Priority to run the thread with</param>
        /// <param name="action">Action to execute</param>
        public static void Execute(ThreadPriority priority, Action action)
        {
            Clean();

            Thread thread = new Thread(new ThreadStart(action));
            thread.Priority = priority;

            thread.Start();
        }

        /// <summary>
        /// Executes a chunk of code on the UI thread
        /// </summary>
        /// <param name="action"></param>
        public static void ExecuteOnUIThread(Action action)
        {
            if (App.Current == null)
                return;

            App.Current.Dispatcher.Invoke(action);
        }

        public static void Clean()
        {
            var deadThreads = new List<Thread>();

            foreach (var thread in _runningThreads)
            {
                if (thread.ThreadState == ThreadState.Stopped)
                    deadThreads.Add(thread);
            }

            foreach (var thread in deadThreads)
                _runningThreads.Remove(thread);
        }

        /// <summary>
        /// Stops all threads that's not yet stopped
        /// </summary>
        public static void Stop()
        {
            foreach (var thread in _runningThreads)
            {
                try
                {
                    if (thread.ThreadState != ThreadState.Stopped)
                        thread.Abort();
                }
                catch (ThreadAbortException)
                {

                }
            }
        }
    }
}
