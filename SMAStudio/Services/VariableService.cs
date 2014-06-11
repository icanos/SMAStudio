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

namespace SMAStudio.Services
{
    sealed class VariableService : BaseService
    {
        private ApiService _api;
        private IList<Variable> _variableCache = null;
        private ObservableCollection<VariableViewModel> _variableViewModelCache = null;

        public VariableService()
        {
            _api = new ApiService();
        }

        public IList<Variable> GetVariables(bool forceDownload = false)
        {
            try
            {
                if (_variableCache == null || forceDownload)
                    _variableCache = _api.Current.Variables.OrderBy(v => v.Name).ToList();

                return _variableCache;
            }
            catch (DataServiceTransportException e)
            {
                Core.Log.Error("Unable to retrieve variables from SMA", e);
                base.NotifyConnectionError();

                return new List<Variable>();
            }
        }

        public ObservableCollection<VariableViewModel> GetVariableViewModels(bool forceDownload = false)
        {
            if (_variableCache == null || forceDownload)
                GetVariables(forceDownload);

            if (_variableViewModelCache != null && !forceDownload)
                return _variableViewModelCache;

            _variableViewModelCache = new ObservableCollection<VariableViewModel>();

            if (_variableCache == null)
                return new ObservableCollection<VariableViewModel>();

            foreach (var variable in _variableCache)
            {
                var viewModel = new VariableViewModel
                {
                    Variable = variable
                };

                _variableViewModelCache.Add(viewModel);
            }

            return _variableViewModelCache;
        }
    }
}
