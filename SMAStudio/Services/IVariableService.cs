using SMAStudio.SMAWebService;
using SMAStudio.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudio.Services
{
    public interface IVariableService
    {
        IList<Variable> GetVariables(bool forceDownload = false);

        ObservableCollection<VariableViewModel> GetVariableViewModels(bool forceDownload = false);

        bool Create();

        bool Update(VariableViewModel runbook);

        bool Delete(VariableViewModel runbook);
    }
}
