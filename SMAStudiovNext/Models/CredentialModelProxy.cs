using SMAStudiovNext.Core;
using SMAStudiovNext.Services;
using System;

namespace SMAStudiovNext.Models
{
    public class CredentialModelProxy : ModelProxyBase, IEnvironmentExplorerItem
    {
        public CredentialModelProxy(object obj, IBackendContext backendContext)
        {
            instance = obj;
            instanceType = instance.GetType();

            Context = backendContext;
        }

        public Guid CredentialID
        {
            get
            {
                var property = GetProperty("CredentialID");
                return (Guid)property.GetValue(instance);
            }
            set
            {
                var property = GetProperty("CredentialID");
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

        public string UserName
        {
            get
            {
                var property = GetProperty("UserName");
                return (string)property.GetValue(instance);
            }
            set
            {
                var property = GetProperty("UserName");
                property.SetValue(instance, value);
            }
        }

        public string RawValue
        {
            get
            {
                var property = GetProperty("RawValue");
                return (string)property.GetValue(instance);
            }
            set
            {
                var property = GetProperty("RawValue");
                property.SetValue(instance, value);
            }
        }
    }
}
