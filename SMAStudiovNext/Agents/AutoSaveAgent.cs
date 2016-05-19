using Caliburn.Micro;
using Gemini.Framework;
using Gemini.Framework.Services;
using Gemini.Modules.Output;
using SMAStudiovNext.Core;
using SMAStudiovNext.Models;
using SMAStudiovNext.Services;
using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using SMAStudiovNext.Modules.WindowRunbook.ViewModels;
using SMAStudiovNext.Utils;

namespace SMAStudiovNext.Agents
{
    public class AutoSaveAgent : IAgent, IDisposable
    {
        private readonly IOutput _output;
        private readonly IShell _shell;
        private readonly IModule _application;

        private object _syncLock = new object();
        private bool _isRunning = true;
        private Thread _backgroundThread;

        public AutoSaveAgent()
        {
            _output = IoC.Get<IOutput>();
            _shell = IoC.Get<IShell>();
            _application = IoC.Get<IModule>();
        }

        public void Start()
        {
            /*if (!Directory.Exists(CacheFolder))
                Directory.CreateDirectory(CacheFolder);

            var files = Directory.GetFiles(CacheFolder);

            if (files.Length > 0)
            {

                _output.AppendLine("Found " + files.Length + " objects to recover.");

                var result = MessageBox.Show("Do you want to restore recovered objects?\r\nIf no, the recovered objects will be forgotten.", "Restore objects?", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    foreach (var file in files)
                    {
                        var fileInfo = new FileInfo(file);
                        var contentReader = new StreamReader(file);
                        var objectContent = contentReader.ReadToEnd();
                        var nameParts = file.Split('_');
                        var contextId = nameParts[0];

                        var context = (_application as Modules.Startup.Module).GetContexts().FirstOrDefault(c => c.ID.Equals(contextId));

                        // If context is null, the context might have been deleted and therefore we discard this
                        if (context == null)
                            continue;

                        contentReader.Close();

                        //AsyncExecution.Run(ThreadPriority.Normal, delegate ()
                        Task.Run(() =>
                        {
                            try
                            {
                                var runbook = context.Runbooks.FirstOrDefault(x => (x.Tag as RunbookModelProxy).RunbookID.ToString().Equals(nameParts[1]));

                                if (runbook == null)
                                {
                                    _output.AppendLine("No runbook was found with ID '" + nameParts[1] + "'.");
                                    return;
                                }

                                if (!(runbook.Tag as RunbookModelProxy).DraftRunbookVersionID.HasValue && MessageBox.Show((runbook.Tag as RunbookModelProxy).RunbookName + " is currently not in edit, do you want to create a draft of the runbook?", "Published Runbook", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                                {
                                    return;
                                }

                                var viewModel = (runbook.Tag as RunbookModelProxy).GetViewModel<RunbookViewModel>();
                                viewModel.AddSnippet(objectContent);
                                //viewModel.Content = objectContent;

                                Execute.OnUIThread(() =>
                                {
                                    _shell.OpenDocument(viewModel);
                                });
                            }
                            catch (DataServiceQueryException ex)
                            {
                                _output.AppendLine("Error when retrieving runbook from backend: " + ex.Message);
                            }
                        });
                    }
                }
                else
                {
                    foreach (var file in files)
                        File.Delete(file);
                }
            }*/

            if (SettingsService.CurrentSettings.EnableLocalCopy)
            {
                _backgroundThread = new Thread(new ThreadStart(StartInternal));
                _backgroundThread.Priority = ThreadPriority.BelowNormal;
                _backgroundThread.Start();
            }
        }

        private void StartInternal()
        {
            try
            {
                while (_isRunning)
                {
                    IList<IDocument> documents = null;

                    lock (_syncLock)
                    {
                        documents = _shell.Documents;
                    }

                    try
                    {
                        foreach (var document in documents)
                        {
                            if (!_isRunning)
                                break;

                            if (!(document is RunbookViewModel))
                                continue;

                            var runbookViewModel = (RunbookViewModel)document;

                            if (!runbookViewModel.UnsavedChanges)
                                continue;

                            try
                            {
                                var path = Path.Combine(SettingsService.CurrentSettings.LocalCopyPath, runbookViewModel.Runbook.Context.ID + "_" + runbookViewModel.Runbook.RunbookName + ".ps1");

                                var textWriter = new StreamWriter(path, false);
                                textWriter.Write(runbookViewModel.Content);
                                textWriter.Flush();
                                textWriter.Close();
                            }
                            catch (IOException)
                            {

                            }
                        }
                    }
                    catch (Exception)
                    {
                        // We might get an exception here if we're closing the application
                        // at the same time as this loop is running.
                    }

                    Thread.Sleep(SettingsService.CurrentSettings.AutoSaveInterval * 1000);
                }
            }
            catch (ThreadAbortException)
            {

            }
        }

        public void Stop()
        {
            _isRunning = false;

            if (_backgroundThread != null)
                _backgroundThread.Abort();

            // Remove cached files if the exit is clean
            /*if (Directory.Exists(CacheFolder))
            {
                var files = Directory.GetFiles(CacheFolder);

                foreach (var file in files)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (IOException)
                    {

                    }
                }
            }*/
        }

        /*private string CacheFolder
        {
            get
            {
                return Path.Combine(AppHelper.CachePath, "cache");
            }
        }*/

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _isRunning = false;

                    // If we close the application in a controlled way, we delete the
                    // cached files since these are onyl used in recovery scenarios.
                    var files = Directory.GetFiles(Path.Combine(AppHelper.CachePath, "cache"));

                    foreach (var file in files)
                        File.Delete(file);
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~AutoSaveAgent() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
