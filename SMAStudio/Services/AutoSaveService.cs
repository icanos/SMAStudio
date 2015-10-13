using SMAStudio.Services;
using SMAStudio.Util;
using SMAStudio.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SMAStudio.Util
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly")]
    public class AutoSaveService : IAutoSaveService, IDisposable
    {
        private bool _running = true;
        private object _sync = new object();

        private IWorkspaceViewModel _workspaceViewModel;
        private IApiService _api;

        public AutoSaveService(IWorkspaceViewModel workspaceViewModel, IApiService api)
        {
            _workspaceViewModel = workspaceViewModel;
            _api = api;
        }

        public void Start()
        {
            string cacheFolder = Path.Combine(AppHelper.CachePath, "cache");

            if (Directory.Exists(cacheFolder))
            {
                var files = Directory.GetFiles(cacheFolder);

                if (files.Length > 0)
                {
                    Core.Log.InfoFormat("Recovered runbooks found.");

                    if (MessageBox.Show("Do you want to restore recovered runbooks?\r\nIf no, the recovered runbooks will be deleted from disk.", "Restore runbooks", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        Core.Log.InfoFormat("User wants to restore runbooks.");

                        // Load the runbooks
                        foreach (var file in files)
                        {
                            var fi = new FileInfo(file);
                            var reader = new StreamReader(file);
                            var content = reader.ReadToEnd();
                            reader.Close();

                            bool isRunbook = fi.Name.StartsWith("rb_") ? true : false;

                            var restoredDocumentGuid = new Guid(fi.Name.Replace("rb_", "").Replace("var_", ""));

                            if (restoredDocumentGuid.Equals(Guid.Empty))
                                continue;

                            if (isRunbook)
                            {
                                AsyncService.Execute(ThreadPriority.BelowNormal, delegate()
                                {
                                    try
                                    {
                                        var runbook = _api.Current.Runbooks.Where(r => r.RunbookID.Equals(restoredDocumentGuid)).FirstOrDefault();

                                        // If we don't find the runbook, this indicates that its either
                                        // removed or we've connected to another environment
                                        if (runbook != null)
                                        {
                                            var runbookViewModel = new RunbookViewModel();
                                            runbookViewModel.Runbook = runbook;
                                            runbookViewModel.Content = content;
                                            runbookViewModel.UnsavedChanges = true;
                                            runbookViewModel.CachedChanges = true; // we do not want to cache this version again, since no changes have been done

                                            var versions = _api.Current.RunbookVersions.Where(rv => rv.RunbookID.Equals(restoredDocumentGuid)).ToList();

                                            foreach (var version in versions)
                                            {
                                                runbookViewModel.Versions.Add(new RunbookVersionViewModel(version));
                                            }

                                            _workspaceViewModel.OpenDocument(runbookViewModel);
                                        }
                                    }
                                    catch (DataServiceQueryException ex)
                                    {
                                        Core.Log.Error("Unable to restore runbook.", ex);
                                    }
                                });
                            }
                            else
                            {
                                // We are loading a variable
                                AsyncService.Execute(ThreadPriority.BelowNormal, delegate()
                                {
                                    var variable = _api.Current.Variables.Where(v => v.VariableID.Equals(restoredDocumentGuid)).FirstOrDefault();

                                    if (variable != null)
                                    {
                                        var variableViewModel = new VariableViewModel();
                                        variableViewModel.Variable = variable;

                                        _workspaceViewModel.OpenDocument(variableViewModel);
                                    }
                                });
                            }
                        }
                    }
                    else
                    {
                        Core.Log.InfoFormat("User did not want to restore runbooks.");

                        foreach (var file in files)
                            File.Delete(file);
                    }
                }
            }

            Thread thread = new Thread(new ThreadStart(InternalStart));
            thread.Priority = ThreadPriority.BelowNormal;
            thread.Start();
        }

        private void InternalStart()
        {
            if (!Directory.Exists(Path.Combine(AppHelper.CachePath, "cache")))
                Directory.CreateDirectory(Path.Combine(AppHelper.CachePath, "cache"));

            while (_running)
            {
                ItemCollection documents = null;

                lock (_sync)
                {
                    documents = MainWindow.Instance.Tabs.Items;
                }

                foreach (IDocumentViewModel document in documents)
                {
                    if (!_running)
                        break;

                    if (!document.UnsavedChanges)
                        continue;

                    if (document.CachedChanges)
                        continue;

                    string prefix = "rb";
                    if (document is VariableViewModel)
                        prefix = "var";

                    try
                    {
                        TextWriter tw = new StreamWriter(Path.Combine(AppHelper.CachePath, "cache", prefix + "_" + document.ID.ToString()), false);
                        tw.Write(document.Content);
                        tw.Flush();
                        tw.Close();
                    }
                    catch (IOException e)
                    {
                        Core.Log.Error("Unable to access cache file for " + document.ID, e);
                    }

                    document.CachedChanges = true;
                }

                Thread.Sleep(10 * 1000);
            }
        }

        public void Dispose()
        {
            _running = false;

            // If we close the application in a controlled way, we delete the
            // cached files since these are onyl used in recovery scenarios.
            var files = Directory.GetFiles(Path.Combine(AppHelper.CachePath, "cache"));

            foreach (var file in files)
                File.Delete(file);
        }
    }
}
