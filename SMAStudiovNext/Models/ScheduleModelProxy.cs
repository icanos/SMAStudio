using SMAStudiovNext.Core;
using SMAStudiovNext.Services;
using System;

namespace SMAStudiovNext.Models
{
    public class ScheduleModelProxy : ModelProxyBase, IEnvironmentExplorerItem
    {
        public ScheduleModelProxy(object obj, IBackendContext backendContext)
        {
            instance = obj;
            instanceType = instance.GetType();

            Context = backendContext;
        }

        public Guid ScheduleID
        {
            get
            {
                var property = GetProperty("ScheduleID");
                return (Guid)property.GetValue(instance);
            }
            set
            {
                var property = GetProperty("ScheduleID");
                property.SetValue(instance, value);
            }
        }

        public string Name
        {
            get
            {
                var property = GetProperty("Name");
                return (string)property.GetValue(instance);
            }
            set
            {
                var property = GetProperty("Name");
                property.SetValue(instance, value);
            }
        }

        public DateTime StartTime
        {
            get
            {
                var property = GetProperty("StartTime");
                return (DateTime)property.GetValue(instance);
            }
            set
            {
                var property = GetProperty("StartTime");
                property.SetValue(instance, value);
            }
        }

        public DateTime? ExpiryTime
        {
            get
            {
                var property = GetProperty("ExpiryTime");
                return (DateTime?)property.GetValue(instance);
            }
            set
            {
                var property = GetProperty("ExpiryTime");
                property.SetValue(instance, value);
            }
        }

        public bool IsEnabled
        {
            get
            {
                var property = GetProperty("IsEnabled");
                return (bool)property.GetValue(instance);
            }
            set
            {
                var property = GetProperty("IsEnabled");
                property.SetValue(instance, value);
            }
        }

        public byte DayInterval
        {
            get
            {
                var property = GetProperty("DayInterval");
                return (byte)property.GetValue(instance);
            }
            set
            {
                var property = GetProperty("DayInterval");
                property.SetValue(instance, value);
            }
        }
    }
}
