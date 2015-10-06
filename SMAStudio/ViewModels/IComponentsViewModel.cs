using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudio.ViewModels
{
    public interface IComponentsViewModel
    {
        void Initialize();

        void Load(bool forceDownload = false, bool cleanFirst = false);

        void AddRunbook(RunbookViewModel runbook);

        void RemoveRunbook(RunbookViewModel runbook);

        void AddVariable(VariableViewModel variable);

        void RemoveVariable(VariableViewModel variable);

        void AddSchedule(ScheduleViewModel schedule);

        void RemoveSchedule(ScheduleViewModel schedule);

        ObservableCollection<RunbookViewModel> Runbooks { get; set; }

        ObservableCollection<VariableViewModel> Variables { get; set; }

        ObservableCollection<CredentialViewModel> Credentials { get; set; }

        ObservableCollection<ScheduleViewModel> Schedules { get; set; }
    }
}
