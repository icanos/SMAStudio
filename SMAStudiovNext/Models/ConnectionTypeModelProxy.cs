using SMAStudiovNext.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Models
{
    public class ConnectionTypeModelProxy : ModelProxyBase
    {
        public ConnectionTypeModelProxy(object obj, IBackendContext backendContext)
        {
            instance = obj;
            instanceType = instance.GetType();

            Context = backendContext;
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

        /// <summary>
        /// This is a collection of connection fields and depending of SMA or Azure, it's either from namespace SMA.* or Vendor.Azure.*
        /// </summary>
        public object ConnectionFields
        {
            get
            {
                var property = GetProperty("ConnectionFields");
                return (object)property.GetValue(instance);
            }
            set
            {
                var property = GetProperty("ConnectionFields");
                property.SetValue(instance, value);
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
