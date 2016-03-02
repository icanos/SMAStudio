using SMAStudiovNext.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Models
{
    public class ConnectionModelProxy : ModelProxyBase
    {
        public ConnectionModelProxy(object obj, IBackendContext backendContext)
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

        public string Description
        {
            get
            {
                var property = GetProperty("Description");
                return (string)property.GetValue(instance);
            }
            set
            {
                var property = GetProperty("Description");
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

        public object ConnectionType
        {
            get
            {
                var property = GetProperty("ConnectionType");
                return (object)property.GetValue(instance);
            }
            set
            {
                var property = GetProperty("ConnectionType");
                property.SetValue(instance, value);
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
