using SMAStudiovNext.Core;
using System;

namespace SMAStudiovNext.Models
{
    public enum RunbookType
    {
        Draft,
        Published
    }

    public class RunbookModelProxy : ModelProxyBase, IEnvironmentExplorerItem
    {
        public RunbookModelProxy(object obj, IBackendContext backendContext)
        {
            instance = obj;
            instanceType = instance.GetType();
            Context = backendContext;
        }

        #region Properties
        /// <summary>
        /// Internal property used for tracking the current job
        /// </summary>
        public Guid JobID
        {
            get; set;
        }

        public string RunbookName
        {
            get
            {
                var property = GetProperty("RunbookName");
                return (string)property.GetValue(instance);
            }
            set
            {
                var property = GetProperty("RunbookName");
                property.SetValue(instance, value);
            }
        }

        public string Tags
        {
            get
            {
                var property = GetProperty("Tags");
                return (string)property.GetValue(instance);
            }
            set
            {
                var property = GetProperty("Tags");
                property.SetValue(instance, value);
            }
        }

        public Guid RunbookID
        {
            get
            {
                var property = GetProperty("RunbookID");
                return (Guid)property.GetValue(instance);
            }
            set
            {
                var property = GetProperty("RunbookID");
                property.SetValue(instance, value);
            }
        }

        public Guid? DraftRunbookVersionID
        {
            get
            {
                var property = GetProperty("DraftRunbookVersionID");
                return (Guid?)property.GetValue(instance);
            }
            set
            {
                var property = GetProperty("DraftRunbookVersionID");
                property.SetValue(instance, value);
            }
        }

        public Guid? PublishedRunbookVersionID
        {
            get
            {
                var property = GetProperty("PublishedRunbookVersionID");
                return (Guid?)property.GetValue(instance);
            }
            set
            {
                var property = GetProperty("PublishedRunbookVersionID");
                property.SetValue(instance, value);
            }
        }

        /// <summary>
        /// Internal property used to know if we're testing a runbook or executing a published version
        /// </summary>
        public bool IsTestRun { get; set; }
        #endregion
    }
}
