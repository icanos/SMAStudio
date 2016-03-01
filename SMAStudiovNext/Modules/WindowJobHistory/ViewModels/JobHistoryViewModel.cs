using Caliburn.Micro;
using Gemini.Framework;
using SMAStudiovNext.Core;
using SMAStudiovNext.Models;
using SMAStudiovNext.Modules.Runbook.ViewModels;
using SMAStudiovNext.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SMAStudiovNext.Modules.JobHistory.ViewModels
{
    public class JobHistoryViewModel : Document, IViewModel
    {
        private readonly RunbookModelProxy _runbook;
        
        public JobHistoryViewModel(RunbookViewModel runbookViewModel)
        {
            _runbook = (RunbookModelProxy)runbookViewModel.Model;
            Owner = _runbook.Context.Service;

            Jobs = new ObservableCollection<JobModelProxy>();

            //AsyncExecution.Run(ThreadPriority.Normal, () =>
            Task.Run(() =>
            {
                IList<JobModelProxy> draftJobs = null;
                IList<JobModelProxy> publishedJobs = null;

                if (_runbook.DraftRunbookVersionID.HasValue)
                    draftJobs = Owner.GetJobs(_runbook.DraftRunbookVersionID.Value);
                
                if (_runbook.PublishedRunbookVersionID.HasValue)
                    publishedJobs = Owner.GetJobs(_runbook.PublishedRunbookVersionID.Value);

                Execute.OnUIThread(() =>
                {
                    if (draftJobs != null)
                    {
                        foreach (var job in draftJobs)
                        {
                            job.BoundRunbookViewModel = runbookViewModel;
                            job.RunbookType = RunbookType.Draft;
                            Jobs.Add(job);
                        }
                    }

                    if (publishedJobs != null)
                    {
                        foreach (var job in publishedJobs)
                        {
                            job.BoundRunbookViewModel = runbookViewModel;
                            job.RunbookType = RunbookType.Published;
                            Jobs.Add(job);
                        }
                    }

                    Jobs = Jobs.OrderBy(j => j.StartTime).ToObservableCollection();
                });
            });
        }

        public ObservableCollection<JobModelProxy> Jobs { get; set; }

        public override string DisplayName
        {
            get
            {
                return "Job History - " + _runbook.RunbookName;
            }
            set
            {
                // Not possible
            }
        }

        public string Content
        {
            get
            {
                return string.Empty;
            }
        }

        public object Model
        {
            get; set;
        }

        public bool UnsavedChanges
        {
            get; set;
        }

        public ICommand LoadCommand
        {
            get { return AppContext.Resolve<ICommand>("LoadCommand"); }
        }

        public IBackendService Owner
        {
            private get; set;
        }
    }
}
