using SMAStudio.SMAWebService;
using SMAStudio.Util;
using SMAStudio.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Services.Client;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SMAStudio.Services
{
    /// <summary>
    /// Service responsible for retrieving runbooks from SMA
    /// </summary>
    sealed class RunbookService : BaseService
    {
        private ApiService _api;
        private IList<Runbook> _runbookCache = null;
        private ObservableCollection<RunbookViewModel> _runbookViewModelCache = null;

        public RunbookService()
        {
            _api = new ApiService();
        }

        public IList<Runbook> GetRunbooks(bool forceDownload = false)
        {
            try
            {
                if (_runbookCache == null || forceDownload)
                    _runbookCache = _api.Current.Runbooks.OrderBy(r => r.RunbookName).ToList();

                return _runbookCache;
            }
            catch (DataServiceTransportException)
            {
                NotifyConnectionError();

                return new List<Runbook>();
            }
        }

        public ObservableCollection<RunbookViewModel> GetRunbookViewModels(bool forceDownload = false)
        {
            if (_runbookCache == null || forceDownload)
                GetRunbooks(forceDownload);

            if (_runbookViewModelCache != null && !forceDownload)
                return _runbookViewModelCache;

            _runbookViewModelCache = new ObservableCollection<RunbookViewModel>();

            if (_runbookCache == null)
                return new ObservableCollection<RunbookViewModel>();

            foreach (var runbook in _runbookCache)
            {
                var viewModel = new RunbookViewModel
                {
                    Runbook = runbook,
                    CheckedOut = runbook.DraftRunbookVersionID.HasValue
                };

                _runbookViewModelCache.Add(viewModel);
            }

            return _runbookViewModelCache;
        }

        public List<RunbookVersionViewModel> GetVersions(RunbookViewModel runbookViewModel)
        {
            try
            {
                var versions = _api.Current.RunbookVersions.Where(rv => rv.RunbookID.Equals(runbookViewModel.Runbook.RunbookID) && !rv.IsDraft).ToList();
                var versionsViewModels = new List<RunbookVersionViewModel>();

                foreach (var version in versions)
                    versionsViewModels.Add(new RunbookVersionViewModel(version));

                return versionsViewModels;
            }
            catch (DataServiceTransportException)
            {
                base.NotifyConnectionError();

                return new List<RunbookVersionViewModel>();
            }
        }
    }
}
