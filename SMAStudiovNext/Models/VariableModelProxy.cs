using SMAStudiovNext.Core;
using SMAStudiovNext.Services;
using System;

namespace SMAStudiovNext.Models
{
    public class VariableModelProxy : ModelProxyBase, IEnvironmentExplorerItem
    {
        public VariableModelProxy(object obj, IBackendContext backendContext)
        {
            instance = obj;
            instanceType = instance.GetType();

            Context = backendContext;
        }

        public Guid VariableID
        {
            get
            {
                var property = GetProperty("VariableID");
                return (Guid)property.GetValue(instance);
            }
            set
            {
                var property = GetProperty("VariableID");
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

        public string Value
        {
            get
            {
                var property = GetProperty("Value");
                return (string)property.GetValue(instance);
            }
            set
            {
                var property = GetProperty("Value");
                property.SetValue(instance, value);
            }
        }

        public bool IsEncrypted
        {
            get
            {
                var property = GetProperty("IsEncrypted");
                return (bool)property.GetValue(instance);
            }
            set
            {
                var property = GetProperty("IsEncrypted");
                property.SetValue(instance, value);
            }
        }
    }
}
