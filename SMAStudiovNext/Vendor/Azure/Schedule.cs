using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Vendor.Azure
{
    public class Schedule
    {
        public Guid ScheduleID { get; set; }

        public string Name { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime? ExpiryTime { get; set; }

        public bool IsEnabled { get; set; }

        public int DayInterval { get; set; }

        /// <summary>
        /// This is unique for Azure, doesn't exist in SMA yet.
        /// </summary>
        public int HourInterval { get; set; }
    }
}
