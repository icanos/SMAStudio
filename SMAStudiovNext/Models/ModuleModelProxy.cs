using SMAStudiovNext.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Models
{
    public class ModuleModelProxy : ModelProxyBase
    {
        public ModuleModelProxy(object obj, IBackendContext backendContext)
        {
            instance = obj;
            instanceType = instance.GetType();

            Context = backendContext;
        }

        public string ModuleName
        {
            get
            {
                var property = GetProperty("ModuleName");
                return (string)property.GetValue(instance);
            }
            set
            {
                var property = GetProperty("ModuleName");
                property.SetValue(instance, value);
            }
        }
    }
}
