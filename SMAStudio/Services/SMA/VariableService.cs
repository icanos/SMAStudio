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
    sealed class VariableService : BaseService, IVariableService
    {
        private IApiService _api;
        private IList<Variable> _variableCache = null;
        private ObservableCollection<VariableViewModel> _variableViewModelCache = null;

        private IWorkspaceViewModel _workspaceViewModel;
        private IEnvironmentExplorerViewModel _componentsViewModel;

        public VariableService()
        {
            _api = Core.Resolve<IApiService>();
            _workspaceViewModel = Core.Resolve<IWorkspaceViewModel>();
            _componentsViewModel = Core.Resolve<IEnvironmentExplorerViewModel>();
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

        public bool Create()
        {
            try
            {
                var newVariable = new VariableViewModel
                {
                    Variable = new SMAWebService.Variable(),
                    CheckedOut = true,
                    UnsavedChanges = true
                };

                newVariable.Variable.Name = string.Empty;
                newVariable.Variable.Value = string.Empty;

                newVariable.Variable.VariableID = Guid.Empty;

                _workspaceViewModel.OpenDocument(newVariable);

                // Reload the data from SMA
                _componentsViewModel.Load(true /* force download */);

                return true;
            }
            catch (Exception ex)
            {
                Core.Log.Error("Unable to create a new variable.", ex);
            }

            return false;
        }

        public bool Update(VariableViewModel variable)
        {
            Variable vari = null;

            try
            {
                if (variable.Variable.VariableID != Guid.Empty)
                {
                    vari = _api.Current.Variables.Where(v => v.VariableID.Equals(variable.Variable.VariableID)).FirstOrDefault();

                    if (vari == null)
                        return false;

                    if (vari.IsEncrypted != variable.IsEncrypted)
                    {
                        MessageBox.Show("You cannot change encryption status of a variable.", "Error");
                        return false;
                    }

                    vari.Name = variable.Variable.Name;
                    vari.Value = variable.Variable.Value;

                    _api.Current.UpdateObject(variable.Variable);
                    _api.Current.SaveChanges();
                }
                else
                {
                    vari = new Variable();

                    vari.Name = variable.Name;
                    vari.Value = variable.Content;
                    vari.IsEncrypted = variable.IsEncrypted;

                    if (vari.IsEncrypted)
                    {
                        vari.Value = JsonConverter.ToJson(variable.Content);
                    }

                    _api.Current.AddToVariables(vari);
                    _api.Current.SaveChanges();

                    variable.Variable = vari;
                }

                variable.UnsavedChanges = false;
                variable.CachedChanges = false;

                _componentsViewModel.AddVariable(variable);

                return true;
            }
            catch (Exception ex)
            {
                Core.Log.Error("Unable to save the variable.", ex);
                MessageBox.Show("An error occurred when saving the variable. Please refer to the logs for more information.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return false;
        }

        public bool Delete(VariableViewModel variableViewModel)
        {
            try
            {
                var variable = _api.Current.Variables.Where(v => v.VariableID == variableViewModel.ID).FirstOrDefault();

                if (variable == null)
                {
                    Core.Log.DebugFormat("Trying to remove a variable that doesn't exist. GUID {0}", variableViewModel.ID);
                    return false;
                }

                _api.Current.DeleteObject(variable);
                _api.Current.SaveChanges();

                // Remove the variable from the list of variables
                if (_componentsViewModel != null)
                    _componentsViewModel.RemoveVariable(variableViewModel);

                // If the variable is open, we close it
                if (_workspaceViewModel != null && _workspaceViewModel.Documents.Contains(variableViewModel))
                    _workspaceViewModel.Documents.Remove(variableViewModel);

                return true;
            }
            catch (Exception ex)
            {
                Core.Log.Error("Unable to remove the variable.", ex);
                MessageBox.Show("An error occurred when trying to remove the variable. Please refer to the logs for more information.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return false;
        }
    }
}
