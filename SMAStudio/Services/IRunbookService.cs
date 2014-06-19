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
    public interface IRunbookService
    {
        IList<Runbook> GetRunbooks(bool forceDownload = false);

        ObservableCollection<RunbookViewModel> GetRunbookViewModels(bool forceDownload = false);

        List<RunbookVersionViewModel> GetVersions(RunbookViewModel runbookViewModel);

        Runbook GetRunbook(string runbookName);

        Runbook GetRunbook(Guid runbookId);

        bool Create();

        bool Update(RunbookViewModel runbook);

        bool Delete(RunbookViewModel runbook);

        bool CheckIn(RunbookViewModel runbook);

        bool CheckOut(RunbookViewModel runbook);
    }
}
