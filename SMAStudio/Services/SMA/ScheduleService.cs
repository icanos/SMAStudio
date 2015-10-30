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

namespace SMAStudio.Services.SMA
{
    public class ScheduleService : BaseService, IScheduleService
    {
        private IApiService _api;
        private IList<Schedule> _scheduleCache = null;
        private ObservableCollection<ScheduleViewModel> _scheduleViewModelCache = null;

        private IWorkspaceViewModel _workspaceViewModel;
        private IEnvironmentExplorerViewModel _componentsViewModel;

        public ScheduleService()
        {
            _api = Core.Resolve<IApiService>();
            _workspaceViewModel = Core.Resolve<IWorkspaceViewModel>();
            _componentsViewModel = Core.Resolve<IEnvironmentExplorerViewModel>();
        }

        public IList<Schedule> GetSchedules(bool forceDownload = false)
        {
            try
            {
                if (_scheduleCache == null || forceDownload)
                    _scheduleCache = _api.Current.Schedules.OrderBy(v => v.Name).ToList();

                return _scheduleCache;
            }
            catch (DataServiceTransportException e)
            {
                Core.Log.Error("Unable to retrieve schedules from SMA", e);
                base.NotifyConnectionError();

                return new List<Schedule>();
            }
        }

        public ObservableCollection<ScheduleViewModel> GetScheduleViewModels(bool forceDownload = false)
        {
            if (_scheduleCache == null || forceDownload)
                GetSchedules(forceDownload);

            if (_scheduleViewModelCache != null && !forceDownload)
                return _scheduleViewModelCache;

            _scheduleViewModelCache = new ObservableCollection<ScheduleViewModel>();

            if (_scheduleCache == null)
                return new ObservableCollection<ScheduleViewModel>();

            foreach (var schedule in _scheduleCache)
            {
                var viewModel = new ScheduleViewModel
                {
                    Schedule = schedule
                    //Variable = variable
                };

                _scheduleViewModelCache.Add(viewModel);
            }

            return _scheduleViewModelCache;
        }

        public bool Create()
        {
            try
            {
                var newSchedule = new ScheduleViewModel
                {
                    Schedule = new DailySchedule(),
                    CheckedOut = true,
                    UnsavedChanges = true
                };

                newSchedule.Schedule.Name = string.Empty;
                newSchedule.Schedule.ScheduleID = Guid.Empty;

                _workspaceViewModel.OpenDocument(newSchedule);

                // Reload the data from SMA
                _componentsViewModel.Load(true /* force download */);

                return true;
            }
            catch (Exception ex)
            {
                Core.Log.Error("Unable to create a new schedule.", ex);
            }

            return false;
        }

        public bool Update(ScheduleViewModel schedule)
        {
            try
            {
                if (schedule.Schedule.ScheduleID != Guid.Empty)
                {
                    /*var sched = _api.Current.Schedules.Where(s => s.ScheduleID.Equals(schedule.Schedule.ScheduleID)).FirstOrDefault();

                    if (sched == null)
                        return false;

                    sched.Name = schedule.Name;
                    sched.StartTime = schedule.StartTime;
                    sched.ExpiryTime = sched.ExpiryTime;
                    sched.IsEnabled = true;

                    if (schedule.Schedule is DailySchedule)
                        ((DailySchedule)sched).DayInterval = (byte)schedule.Interval;

                    _api.Current.UpdateObject(schedule.Schedule);
                    _api.Current.SaveChanges();*/
                    Core.Log.ErrorFormat("Currently there is no support for updating a schedule. Please remove it and recreate it.");
                }
                else
                {
                    Schedule sched = schedule.IsDaily ? (Schedule)new DailySchedule() : (Schedule)new OneTimeSchedule();

                    sched.Name = schedule.Name;
                    sched.StartTime = schedule.StartTime;
                    sched.ExpiryTime = sched.ExpiryTime;
                    sched.IsEnabled = true;

                    if (schedule.Schedule is DailySchedule)
                        ((DailySchedule)sched).DayInterval = (byte)schedule.Interval;

                    _api.Current.AddToSchedules(sched);
                    _api.Current.SaveChanges();

                    schedule.Schedule = sched;
                }

                schedule.UnsavedChanges = false;
                schedule.CachedChanges = false;

                _componentsViewModel.AddSchedule(schedule);

                return true;
            }
            catch (Exception ex)
            {
                Core.Log.Error("Unable to save the schedule.", ex);
                MessageBox.Show("An error occurred when saving the schedule. Please refer to the logs for more information.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return false;
        }

        public bool Delete(ScheduleViewModel scheduleViewModel)
        {
            try
            {
                var schedule = _api.Current.Schedules.Where(s => s.ScheduleID == scheduleViewModel.ID).FirstOrDefault();

                if (schedule == null)
                {
                    Core.Log.DebugFormat("Trying to remove a schedule that doesn't exist. GUID {0}", scheduleViewModel.ID);
                    return false;
                }

                _api.Current.DeleteObject(schedule);
                _api.Current.SaveChanges();

                // Remove the variable from the list of variables
                if (_componentsViewModel != null)
                    _componentsViewModel.RemoveSchedule(scheduleViewModel);

                // If the variable is open, we close it
                if (_workspaceViewModel != null && _workspaceViewModel.Documents.Contains(scheduleViewModel))
                    _workspaceViewModel.Documents.Remove(scheduleViewModel);

                return true;
            }
            catch (Exception ex)
            {
                Core.Log.Error("Unable to remove the schedule.", ex);
                MessageBox.Show("An error occurred when trying to remove the schedule. Please refer to the logs for more information.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return false;
        }
    }
}
