using SMAStudio.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SMAStudio.Analysis
{
    /// <summary>
    /// Scans through all runbooks in the environment and retrieves the parameters
    /// that is bound to each runbook.
    /// </summary>
    public class ParameterParserService : IParameterParserService, IDisposable
    {
        private IDictionary<string, IList<UIInputParameter>> _parameterCache;
        private IEnvironmentExplorerViewModel _componentsViewModel;
        private Thread _thread;

        private bool _isRunning = true;
        private bool _hasDiscoveredChanges = true;

        public ParameterParserService()
        {
            _parameterCache = new Dictionary<string, IList<UIInputParameter>>();
        }

        public void Start()
        {
            // Since IEnv.. isn't initialized when constructing this class, we need to resolve this here instead.
            _componentsViewModel = Core.Resolve<IEnvironmentExplorerViewModel>();

            _thread = new Thread(new ThreadStart(delegate ()
            {
                Thread.Sleep(10 * 1000);

                while (_isRunning)
                {
                    if (_hasDiscoveredChanges)
                    {
                        try
                        {
                            foreach (var runbook in _componentsViewModel.Runbooks)
                            {
                                var parameters = runbook.GetParameters(true);

                                if (parameters == null)
                                    continue;

                                if (_parameterCache.ContainsKey(runbook.RunbookName))
                                    _parameterCache[runbook.RunbookName] = parameters;
                                else
                                    _parameterCache.Add(runbook.RunbookName, parameters);
                            }

                            _hasDiscoveredChanges = false;
                        }
                        catch (Exception)
                        {
                            // Silently continue
                        }
                    }

                    Thread.Sleep(5000);
                }
            }));

            _thread.Priority = ThreadPriority.BelowNormal;
            _thread.Start();
        }

        public IList<UIInputParameter> GetParameters(string runbookName)
        {
            if (_parameterCache.ContainsKey(runbookName))
                return _parameterCache[runbookName];

            return new List<UIInputParameter>();
        }

        /// <summary>
        /// Notifies the parameter scanning system of changes
        /// </summary>
        public void NotifyChanges()
        {
            lock (_thread)
            {
                _hasDiscoveredChanges = true;
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    _isRunning = false;
                    _thread.Abort();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~ParameterParserService() {
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
