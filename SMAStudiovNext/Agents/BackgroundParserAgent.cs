using Caliburn.Micro;
using Gemini.Framework.Services;
using Gemini.Modules.ErrorList;
using Gemini.Modules.Output;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SMAStudiovNext.Modules.WindowRunbook.ViewModels;
using Microsoft.Windows.PowerShell.ScriptAnalyzer;
using System.Windows;
using System;

namespace SMAStudiovNext.Agents
{
    /// <summary>
    /// Background parser for the currently active runbook
    /// </summary>
    public class BackgroundParserAgent : IAgent
    {
        private readonly IShell _shell;

        private readonly object _syncLock = new object();
        private readonly Thread _backgroundThread;
        private bool _isRunning = true;
        
        public BackgroundParserAgent()
        {
            _backgroundThread = new Thread(new ThreadStart(StartInternal)) {Priority = ThreadPriority.BelowNormal};
            _shell = IoC.Get<IShell>();
        }

        /// <summary>
        /// Entry point for agent
        /// </summary>
        public void Start()
        {
            //_backgroundThread.Start();
            //Task.Run(async () => { await StartInternal(); });
        }

        /// <summary>
        /// Runs our background thread where the parsing of the runbook is done. If there is parse errors in our runbook,
        /// tokens will be null and we won't be able to populate our auto complete engine with information about variables etc.
        /// </summary>
        private void StartInternal()
        {
            while (_isRunning)
            {
                var runbook = _shell.ActiveItem as RunbookViewModel;

                runbook?.ParseContent();
                Thread.Sleep(2 * 1000);
            }
        }
        
        /// <summary>
        /// Called when the agent should stop
        /// </summary>
        public void Stop()
        {
            lock (_syncLock)
            {
                _isRunning = true;
                _backgroundThread.Abort();
            }
        }
    }
}
