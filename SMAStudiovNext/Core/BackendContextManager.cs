using Caliburn.Micro;
using SMAStudiovNext.Modules.PartEnvironmentExplorer.ViewModels;
using SMAStudiovNext.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SMAStudiovNext.Core
{
    /// <summary>
    /// Responsible for all backend contexts
    /// </summary>
    [Export(typeof(IBackendContextManager))]
    public class BackendContextManager : IBackendContextManager
    {
        private IList<BackendContext> _backendContexts;

        public BackendContextManager()
        {
            _backendContexts = new List<BackendContext>();
        }

        public void Initialize()
        {
            if (SettingsService.CurrentSettings == null)
                throw new InvalidOperationException("Settings needs to be loaded first.");

            foreach (var conn in SettingsService.CurrentSettings.Connections)
            {
                Load(conn);
            }
        }

        public BackendContext Load(BackendConnection connection)
        {
            var contextType = ContextType.SMA;

            if (connection.IsAzure)
                contextType = ContextType.Azure;
            else if (connection.IsAzureRM)
                contextType = ContextType.AzureRM;

            var existingBackend = _backendContexts.FirstOrDefault(c => c.ID == connection.Id);

            if (existingBackend != null)
                return existingBackend;

            var environment = IoC.Get<EnvironmentExplorerViewModel>();
            var backend = new BackendContext(contextType, connection);
            _backendContexts.Add(backend);

            Execute.OnUIThread(() => environment.Items.Add(backend.GetStructure()));

            backend.OnLoaded += OnBackendReady;

            return backend;
        }

        public void Refresh()
        {
            foreach (var context in _backendContexts)
            {
                if (!context.IsReady)
                    continue;
                
                context.Tags.Clear();
                context.Start();
            }
        }

        /// <summary>
        /// Gets called when a backend finishes loading
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnBackendReady(object sender, ContextUpdatedEventArgs e)
        {
            var environment = IoC.Get<EnvironmentExplorerViewModel>();

            foreach (var context in _backendContexts)
            {
                if (context.Exception != null)
                {
                    Execute.OnUIThread(() =>
                    {
                        var message = context.Exception.Message;

                        if (context.Exception.InnerException != null)
                            message += "\r\n\r\n" + context.Exception.InnerException.Message;

                        MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        AppContext.Resolve<IStatusManager>().SetText("");
                        LongRunningOperation.Stop();
                    });
                }
            }

            // We need to update the environment object
            if (environment != null)
            {
                environment.OnBackendReady(sender, e);

                var item = environment.Items.FirstOrDefault(i => i.Title.Equals(e.Context.Name));
                var idx = environment.Items.IndexOf(item);

                environment.Items[idx].Items.Clear();

                if (item != null)
                {
                    var tree = e.Context.GetStructure();
                    environment.Items[idx].Icon = tree.Icon;

                    foreach (var treeItem in tree.Items)
                        environment.Items[idx].Items.Add(treeItem);
                }
            }

            bool allContextsReady = true;
            foreach (var context in _backendContexts)
            {
                if (!context.IsReady)
                {
                    allContextsReady = false;
                    break;
                }
            }

            Execute.OnUIThread(() =>
            {
                if (allContextsReady)
                    AppContext.Resolve<IStatusManager>().SetText("");

                // Cancel the spinner that shows we're loading data
                LongRunningOperation.Stop();
            });
        }
    }
}
