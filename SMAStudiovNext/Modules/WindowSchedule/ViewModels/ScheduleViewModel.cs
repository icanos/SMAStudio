using Gemini.Framework;
using Gemini.Framework.Commands;
using SMAStudiovNext.Core;
using SMAStudiovNext.Models;
using SMAStudiovNext.Modules.Shell.Commands;
using SMAStudiovNext.Services;
using System;
using System.Windows;
using System.Threading.Tasks;

namespace SMAStudiovNext.Modules.Schedule.ViewModels
{
    public sealed class ScheduleViewModel : Document, IViewModel, ICommandHandler<SaveCommandDefinition>
    {
        private readonly ScheduleModelProxy model;

        public ScheduleViewModel(ScheduleModelProxy schedule)
        {
            model = schedule;

            if (schedule.ScheduleID == Guid.Empty)
            {
                UnsavedChanges = true;
            }

            Owner = schedule.Context.Service;
        }

        public override void CanClose(Action<bool> callback)
        {
            if (UnsavedChanges)
            {
                var result = MessageBox.Show("There are unsaved changes in the schedule object, changes will be lost. Do you want to continue?", "Unsaved changes", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                {
                    callback(false);
                    return;
                }
            }

            callback(true);
        }

        void ICommandHandler<SaveCommandDefinition>.Update(Command command)
        {
            if (UnsavedChanges)
                command.Enabled = true;
            else
                command.Enabled = false;
        }

        async Task ICommandHandler<SaveCommandDefinition>.Run(Command command)
        {
            await Task.Run(delegate ()
            {
                Owner.Save(this);

                model.ViewModel = this;

                //var backendContext = AppContext.Resolve<IBackendContext>();
                //backendContext.AddToSchedules(model);
                Owner.Context.AddToSchedules(model);

                // Update the UI to notify that the changes has been saved
                UnsavedChanges = false;
                NotifyOfPropertyChange(() => DisplayName);
            });
        }

        #region Properties
        public override string DisplayName
        {
            get { return UnsavedChanges ? Name + "*" : Name; }
        }

        public string Name
        {
            get
            {
                return model.Name;
            }
            set
            {
                model.Name = value;
                NotifyOfPropertyChange(() => DisplayName);
                NotifyOfPropertyChange(() => IsReadOnly);
            }
        }

        public DateTime StartTime
        {
            get
            {
                return model.StartTime;
            }
            set
            {
                if (model.StartTime != null && !model.StartTime.Equals(value))
                    UnsavedChanges = true;

                model.StartTime = value;

                NotifyOfPropertyChange(() => DisplayName);
            }
        }

        public DateTime? ExpiryTime
        {
            get
            {
                return model.ExpiryTime;
            }
            set
            {
                if (model.ExpiryTime != null && !model.ExpiryTime.Equals(value))
                    UnsavedChanges = true;

                model.ExpiryTime = value;

                NotifyOfPropertyChange(() => DisplayName);
            }
        }

        public bool IsEnabled
        {
            get
            {
                return model.IsEnabled;
            }
            set
            {
                if (!model.IsEnabled.Equals(value))
                    UnsavedChanges = true;

                model.IsEnabled = value;

                NotifyOfPropertyChange(() => DisplayName);
            }
        }

        public bool IsDaily
        {
            get; set;
        }

        public byte DayInterval
        {
            get
            {
                return model.DayInterval;
            }
            set
            {
                if (!model.DayInterval.Equals(value))
                    UnsavedChanges = true;

                model.DayInterval = value;

                NotifyOfPropertyChange(() => DisplayName);
            }
        }

        /// <summary>
        /// If we are editing a existing schedule, we are not allowed to change
        /// the any parameters of the Schedule and therefor return true.
        /// </summary>
        public bool IsReadOnly
        {
            get { return model.ScheduleID == Guid.Empty ? false : true; }
        }

        public bool IsInvertedReadOnly
        {
            get { return !IsReadOnly; }
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
            get
            {
                return model;
            }
            set
            {
                // Cannot be assigned
                throw new NotSupportedException();
            }
        }

        public bool UnsavedChanges
        {
            get; set;
        }

        public IBackendService Owner
        {
            private get; set;
        }
        #endregion
    }
}
