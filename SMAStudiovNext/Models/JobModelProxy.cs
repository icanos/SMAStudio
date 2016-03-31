using SMAStudiovNext.Core;
using SMAStudiovNext.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SMAStudiovNext.Modules.WindowRunbook.ViewModels;

namespace SMAStudiovNext.Models
{
    public class JobModelProxy : ModelProxyBase
    {
        public JobModelProxy(object obj, IBackendContext backendContext)
        {
            instance = obj;
            instanceType = instance.GetType();

            Result = new List<JobOutput>();
            Context = backendContext;
        }

        public Guid JobID
        {
            get
            {
                var property = GetProperty("JobID");
                return (Guid)property.GetValue(instance);
            }
            set
            {
                var property = GetProperty("JobID");
                property.SetValue(instance, value);
            }
        }

        public string JobStatus
        {
            get
            {
                var property = GetProperty("JobStatus");
                return (string)property.GetValue(instance);
            }
            set
            {
                var property = GetProperty("JobStatus");
                property.SetValue(instance, value);
            }
        }

        public string JobException
        {
            get
            {
                var property = GetProperty("JobException");
                return (string)property.GetValue(instance);
            }
            set
            {
                var property = GetProperty("JobException");
                property.SetValue(instance, value);
            }
        }

        public DateTime? StartTime
        {
            get
            {
                var property = GetProperty("StartTime");
                return (DateTime?)property.GetValue(instance);
            }
            set
            {
                var property = GetProperty("StartTime");
                property.SetValue(instance, value);
            }
        }

        public DateTime? EndTime
        {
            get
            {
                var property = GetProperty("EndTime");
                return (DateTime?)property.GetValue(instance);
            }
            set
            {
                var property = GetProperty("EndTime");
                property.SetValue(instance, value);
            }
        }

        public DateTime CreationTime
        {
            get
            {
                var property = GetProperty("CreationTime");
                return (DateTime)property.GetValue(instance);
            }
            set
            {
                var property = GetProperty("CreationTime");
                property.SetValue(instance, value);
            }
        }

        public DateTime LastModifiedTime
        {
            get
            {
                var property = GetProperty("LastModifiedTime");
                return (DateTime)property.GetValue(instance);
            }
            set
            {
                var property = GetProperty("LastModifiedTime");
                property.SetValue(instance, value);
            }
        }

        public short? ErrorCount
        {
            get
            {
                var property = GetProperty("ErrorCount");
                return (short?)property.GetValue(instance);
            }
            set
            {
                var property = GetProperty("ErrorCount");
                property.SetValue(instance, value);
            }
        }

        public short? WarningCount
        {
            get
            {
                var property = GetProperty("WarningCount");
                return (short?)property.GetValue(instance);
            }
            set
            {
                var property = GetProperty("WarningCount");
                property.SetValue(instance, value);
            }
        }

        /// <summary>
        /// Custom parameter that contains the parameters with which this job was started
        /// </summary>
        public Dictionary<string, string> Parameters { get; set; }

        /// <summary>
        /// Internal property to know when we last downloaded results from the job (this is to only get new data)
        /// </summary>
        public DateTime LastDownloadTime
        {
            get; set;
        }

        /// <summary>
        /// Internal property to contain the output of our job
        /// </summary>
        public IList<JobOutput> Result
        {
            get; set;
        }

        /// <summary>
        /// This is populated when executing from History view, otherwise null. Probably should do it always, right?
        /// </summary>
        public RunbookViewModel BoundRunbookViewModel { get; set; }

        /// <summary>
        /// Internal property used to inform about what type of runbook the job was executed against (Draft/Published)
        /// </summary>
        public RunbookType RunbookType { get; set; }
    }
}
