using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudio.Editor.CodeCompletion
{
    public class PowershellCompletion
    {
        public PowershellCompletion()
        {
            var modulesPath = new List<string>
            {
                @"C:\Windows\SysWOW64\WindowsPowerShell\v1.0\Modules",
                @"C:\Windows\System32\WindowsPowerShell\v1.0\Modules"
            };

            Runspace runspace = RunspaceFactory.CreateRunspace();
            var assemblies = runspace.RunspaceConfiguration.Assemblies;

            //var unresolvableAssemblies=new IUnresolvableAssembly
            Stopwatch total = Stopwatch.StartNew();
            Parallel.For(
                0,
                assemblies.Count,
                delegate(int i)
                {
                    var assembly = Assembly.LoadFile(assemblies[i].FileName);
                    var test = assembly.GetTypes();
                    //var test = assemblies[i].FileName;
                });
        }
    }
}
