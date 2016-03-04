using Caliburn.Micro;
using Gemini.Framework;
using Gemini.Framework.Commands;
using Gemini.Framework.Services;
using Gemini.Framework.Threading;
using Gemini.Modules.Output;
using SMAStudiovNext.Commands;
using SMAStudiovNext.Core;
using SMAStudiovNext.Icons;
using SMAStudiovNext.Models;
using SMAStudiovNext.Modules.PropertyGrid;
using SMAStudiovNext.Modules.Runbook.Commands;
using SMAStudiovNext.Modules.Runbook.ViewModels;
using SMAStudiovNext.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SMAStudiovNext.Modules.ExecutionResult.ViewModels
{
    public sealed class ExecutionResultViewModel : Document, ICommandHandler<PauseCommandDefinition>, ICommandHandler<StopCommandDefinition>
    {
        private readonly RunbookViewModel _runbookViewModel;
        private readonly IBackendService _backendService;
        private readonly IPropertyGrid _inspectorTool;
        private readonly IList<string> _completedExecutionStatus = new List<string> { "Completed", "Failed", "Stopped" };
        private Guid _jobId;

        private ExecutionResultPropertyInfo _propertyInfo;
        private string _displayName = string.Empty;
        private bool _shouldClosePropertyGridOnDeactivate = false;

        public ExecutionResultViewModel()
        {
            Result = new ObservableCollection<JobOutput>();

            // Display the property info tool if not visible yet
            var shell = IoC.Get<IShell>();
            var propertyTool = shell.Tools.FirstOrDefault(t => t is IPropertyGrid);

            if (propertyTool == null || (propertyTool != null && !propertyTool.IsVisible))
            {
                if (propertyTool == null)
                    propertyTool = IoC.Get<IPropertyGrid>();

                _shouldClosePropertyGridOnDeactivate = true;
                shell.ShowTool(propertyTool);
            }

            _inspectorTool = (IPropertyGrid)propertyTool;
        }

        public ExecutionResultViewModel(RunbookViewModel runbookViewModel)
            : this()
        {
            _runbookViewModel = runbookViewModel;
            _jobId = ((RunbookModelProxy)_runbookViewModel.Model).JobID;

            _backendService = (_runbookViewModel.Model as RunbookModelProxy).Context.Service;

            SubscribeToJob();
        }

        public ExecutionResultViewModel(RunbookViewModel runbookViewModel, Guid jobId)
            : this()
        {
            _runbookViewModel = runbookViewModel;
            _jobId = jobId;

            _backendService = (_runbookViewModel.Model as RunbookModelProxy).Context.Service;

            SubscribeToJob();
        }

        public override void TryClose(bool? dialogResult = default(bool?))
        {
            var shell = IoC.Get<IShell>();
            var propertyTool = shell.Tools.FirstOrDefault(t => t is IPropertyGrid);

            if (propertyTool != null && _shouldClosePropertyGridOnDeactivate)
            {
                shell.Tools.Remove(propertyTool);
            }

            Result.Clear();
            Result = null;

            _propertyInfo = null;

            base.TryClose(dialogResult);
        }

        private void SubscribeToJob()
        {
            var job = default(JobModelProxy);

            Task.Run(() =>
            {
                try
                {
                    //AsyncExecution.Run(ThreadPriority.Normal, () =>
                    // Wait for the job ID to be set by our backend service
                    while (_jobId == Guid.Empty)
                    {
                        if (_runbookViewModel.Runbook.JobID != null)
                            _jobId = _runbookViewModel.Runbook.JobID;

                        Thread.Sleep(1 * 1000);
                    }

                    if (_runbookViewModel.Runbook.JobID != Guid.Empty)
                        job = _backendService.GetJobDetails(_runbookViewModel.Runbook);
                    else if (_jobId != Guid.Empty)
                        job = _backendService.GetJobDetails(_jobId);

                    if (job != null)
                    {
                        Execute.OnUIThread(() =>
                        {
                            foreach (var entry in job.Result)
                                Result.Add(entry);

                            JobStatus = job.JobStatus;
                            NotifyOfPropertyChange(() => DisplayName);

                            _propertyInfo = new ExecutionResultPropertyInfo();
                            _propertyInfo.JobID = (_jobId == null) ? Guid.Empty : (Guid)_jobId;
                            _propertyInfo.RunbookID = (_runbookViewModel != null) ? ((RunbookModelProxy)_runbookViewModel.Model).RunbookID : Guid.Empty;
                            _propertyInfo.RunbookName = (_runbookViewModel != null) ? ((RunbookModelProxy)_runbookViewModel.Model).RunbookName : "Unknown";
                            _propertyInfo.JobStatus = job.JobStatus;
                            _propertyInfo.StartTime = job.StartTime;
                            _propertyInfo.EndTime = job.EndTime;
                            _propertyInfo.CreationTime = job.CreationTime;
                            _propertyInfo.LastModifiedTime = job.LastModifiedTime;
                            _propertyInfo.ErrorCount = job.ErrorCount;
                            _propertyInfo.WarningCount = job.WarningCount;
                            _propertyInfo.Exception = job.JobException;

                            _inspectorTool.SelectedObject = _propertyInfo;
                        });
                    }

                    bool hasDisplayedException = false;
                    while (!_completedExecutionStatus.Contains(job.JobStatus))
                    {
                        job = _backendService.GetJobDetails(_runbookViewModel.Runbook);

                        if (job != null)
                        {
                            Execute.OnUIThread(() =>
                            {
                                JobStatus = job.JobStatus;
                                NotifyOfPropertyChange(() => DisplayName);

                                _propertyInfo.StartTime = job.StartTime;
                                _propertyInfo.EndTime = job.EndTime;
                                _propertyInfo.ErrorCount = job.ErrorCount;
                                _propertyInfo.WarningCount = job.WarningCount;
                                _propertyInfo.JobStatus = job.JobStatus;
                                _propertyInfo.Exception = job.JobException;

                                if (!String.IsNullOrEmpty(job.JobException) && !hasDisplayedException)
                                {
                                    var output = IoC.Get<IOutput>();
                                    output.AppendLine("Error when executing runbook:");
                                    output.AppendLine(job.JobException);
                                    output.AppendLine(" ");

                                    hasDisplayedException = true;
                                }

                                _inspectorTool.SelectedObject = null;
                                _inspectorTool.SelectedObject = _propertyInfo;

                                foreach (var entry in job.Result)
                                    Result.Add(entry);
                            });
                        }

                        Thread.Sleep(5 * 1000);
                    }

                    // The job is completed
                    _runbookViewModel.Runbook.JobID = Guid.Empty;
                }
                catch (ApplicationException ex)
                {
                    GlobalExceptionHandler.Show(ex);
                    _runbookViewModel.Runbook.JobID = Guid.Empty;

                    job.JobStatus = "Failed";
                }
            });

            if (job != null)
            {
                var output = IoC.Get<IOutput>();
                output.AppendLine("Job executed with status: " + job.JobStatus);
            }
        }

        void ICommandHandler<PauseCommandDefinition>.Update(Command command)
        {
            if (!_completedExecutionStatus.Contains(JobStatus))
            {
                command.Enabled = true;

                if (JobStatus != null && JobStatus.Equals("Suspended"))
                {
                    // This means that the runbook is already paused, change the button to "Resume"-state
                    command.IconSource = new Uri("pack://application,,," + IconsDescription.Resume);
                    command.Text = "Resume";
                    command.ToolTip = "Resume Execution";
                }
                else if (command.Text.Equals("Resume"))
                {
                    // Restore the button to Pause state
                    command.IconSource = new Uri("pack://application,,," + IconsDescription.Pause);
                    command.Text = "Pause";
                    command.ToolTip = "Pause Execution";
                }
            }
            else
                command.Enabled = false;
        }

        Task ICommandHandler<PauseCommandDefinition>.Run(Command command)
        {
            var backendService = (_runbookViewModel.Model as RunbookModelProxy).Context.Service;

            try
            {
                if (command.Text.Equals("Pause"))
                    backendService.PauseExecution(_jobId);
                else
                    backendService.ResumeExecution(_jobId);
            }
            catch (ApplicationException ex)
            {
                GlobalExceptionHandler.Show(ex);
            }

            return TaskUtility.Completed;
        }

        #region ICommandHandler<StopCommandDefinition>
        void ICommandHandler<StopCommandDefinition>.Update(Command command)
        {
            if (!_completedExecutionStatus.Contains(JobStatus))
                command.Enabled = true;
            else
                command.Enabled = false;
        }

        Task ICommandHandler<StopCommandDefinition>.Run(Command command)
        {
            //var _backendService = AppContext.Resolve<IBackendService>();
            //_backendService.StopExecution(_jobId);
            var backendService = (_runbookViewModel.Model as RunbookModelProxy).Context.Service;

            try
            {
                backendService.StopExecution(_jobId);
            }
            catch (ApplicationException ex)
            {
                GlobalExceptionHandler.Show(ex);
            }

            return TaskUtility.Completed;
        }
        #endregion

        public override string DisplayName
        {
            get
            {
                var name = "Execution: " + ((RunbookModelProxy)_runbookViewModel.Model).RunbookName;

                if (!_completedExecutionStatus.Contains(JobStatus))
                    name += " (" + (!String.IsNullOrEmpty(JobStatus) ? JobStatus : "Starting") + ")";

                return name;
            }
            set
            {
                // Not possible
            }
        }

        public string JobStatus
        {
            get; set;
        }

        public ObservableCollection<JobOutput> Result { get; set; }
    }
    
    [DisplayName("Execution Details")]
    public class ExecutionResultPropertyInfo : PropertyChangedBase
    {
        [Category("Job")]
        [DisplayName("ID")]
        public Guid JobID { get; set; }

        [Category("Runbook")]
        [DisplayName("ID")]
        public Guid RunbookID { get; set; }

        [Category("Runbook")]
        [DisplayName("Name")]
        public string RunbookName { get; set; }

        [Category("Job")]
        [DisplayName("Status")]
        public string JobStatus { get; set; }

        [Category("Execution")]
        [DisplayName("Started")]
        public DateTime? StartTime { get; set; }

        [Category("Execution")]
        [DisplayName("Ended")]
        public DateTime? EndTime { get; set; }

        [Category("Execution")]
        [DisplayName("Created")]
        public DateTime CreationTime { get; set; }

        [Category("Execution")]
        [DisplayName("Last Modified")]
        public DateTime LastModifiedTime { get; set; }

        [Category("Job")]
        [DisplayName("Errors")]
        public short? ErrorCount { get; set; }

        [Category("Job")]
        [DisplayName("Warnings")]
        public short? WarningCount { get; set; }

        [Category("Job")]
        [DisplayName("Exception")]
        public string Exception { get; set; }

        public override string ToString()
        {
            return "Job: " + RunbookName;
        }
    }
}
