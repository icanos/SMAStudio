using SMAStudio.Models;
using SMAStudio.Resources;
using SMAStudio.SMAWebService;
using SMAStudio.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;

namespace SMAStudio.ViewModels
{
    public class ScheduleViewModel : ObservableObject, IDocumentViewModel
    {
        private Schedule _schedule = null;
        private bool _unsavedChanges = false;
        private string _icon = Icons.Schedule;

        public ScheduleViewModel()
        {
            
        }

        public void DocumentLoaded()
        {
            
        }

        public void TextChanged(object sender, EventArgs e)
        {
            //throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the variable name accompanied wth a asterisk (*) if the schedule contains
        /// unsaved data
        /// </summary>
        public string Title
        {
            get
            {
                string scheduleName = Schedule.Name;

                if (String.IsNullOrEmpty(scheduleName))
                    scheduleName += "untitled";

                if (UnsavedChanges)
                    scheduleName += "*";

                return scheduleName;
            }
            set { Schedule.Name = value; UnsavedChanges = true; }
        }

        /// <summary>
        /// Gets or sets the name of the schedule
        /// </summary>
        public string Name
        {
            get
            {
                return Schedule.Name;
            }
            set { Schedule.Name = value; UnsavedChanges = true; }
        }

        /// <summary>
        /// If we are editing a existing schedule, we are not allowed to change
        /// the any parameters of the Schedule and therefor return true.
        /// </summary>
        public bool IsReadOnly
        {
            get { return ID == Guid.Empty ? false : true; }
        }

        /// <summary>
        /// Gets or sets the variable model object
        /// </summary>
        public Schedule Schedule
        {
            get { return _schedule; }
            set
            {
                _schedule = value;
                base.RaisePropertyChanged("Title");
                base.RaisePropertyChanged("Name");
            }
        }

        public Guid ID
        {
            get { return _schedule.ScheduleID; }
            set { _schedule.ScheduleID = value; }
        }

        public DateTime StartTime
        {
            get { return _schedule.StartTime; }
            set { _schedule.StartTime = value.ToUniversalTime(); UnsavedChanges = true; }
        }

        public DateTime? ExpiryTime
        {
            get { return _schedule.ExpiryTime; }
            set { _schedule.ExpiryTime = value.HasValue ? value.Value.ToUniversalTime() : value; UnsavedChanges = true; }
        }

        public bool IsDaily
        {
            get { return _schedule is DailySchedule; }
            set
            {
                if (_schedule is DailySchedule)
                    return;

                var sched = new DailySchedule();
                sched.StartTime = StartTime;
                sched.ExpiryTime = ExpiryTime;
                sched.IsEnabled = true;
                sched.DayInterval = 1;

                _schedule = sched;

                UnsavedChanges = true;

                base.RaisePropertyChanged("Title");
                base.RaisePropertyChanged("Name");
                base.RaisePropertyChanged("StartTime");
                base.RaisePropertyChanged("ExpiryTime");
                base.RaisePropertyChanged("IsDaily");
            }
        }

        public int Interval
        {
            get
            {
                if (_schedule is DailySchedule)
                    return (int)((DailySchedule)_schedule).DayInterval;

                return 0;
            }
            set
            {
                if (_schedule is DailySchedule && value > 0)
                {
                    ((DailySchedule)_schedule).DayInterval = (byte)value;
                    UnsavedChanges = true;
                }
                else if (value == 0 && IsDaily)
                    MessageBox.Show("Interval needs to be at between 1 and 128 when recurring schedule is enabled.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        /// <summary>
        /// Used to mark the tab with an asterisk if the schedule has unsaved changes.
        /// </summary>
        public bool UnsavedChanges
        {
            get { return _unsavedChanges; }
            set
            {
                if (_unsavedChanges.Equals(value))
                    return;

                _unsavedChanges = value;

                // Set the CachedChanges to false in order for our auto saving engine to store a
                // local copy in case the application crashes
                CachedChanges = false;

                base.RaisePropertyChanged("Name");
                base.RaisePropertyChanged("Title");
            }
        }

        /// <summary>
        /// Set to true to notify the auto saving system that this schedule has unsaved changes
        /// that should be cached on local hard drive.
        /// </summary>
        public bool CachedChanges
        {
            get;
            set;
        }

        /// <summary>
        /// Will always return true since a schedule is always checked out
        /// </summary>
        public bool CheckedOut
        {
            get { return true; }
            set { }
        }

        /// <summary>
        /// Not implemented for schedules
        /// </summary>
        public string Content
        {
            get;
            set;
        }

        /// <summary>
        /// Icon for a schedule
        /// </summary>
        public string Icon
        {
            get { return _icon; }
            set { _icon = value; }
        }

        /// <summary>
        /// Not implemented for schedules, this is used to determine if the user has stopped
        /// writing or not (to cache changes to local hard drive)
        /// </summary>
        public DateTime LastTimeKeyDown
        {
            get;
            set;
        }

        /// <summary>
        /// Holds information about whether or not the node is expanded in the treeview
        /// </summary>
        public bool IsExpanded
        {
            get;
            set;
        }

        /// <summary>
        /// Unused for schedules
        /// </summary>
        public ObservableCollection<DocumentReference> References
        {
            get;
            set;
        }
    }
}
