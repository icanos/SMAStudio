using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudio.Models
{
    public interface IProjectContent
    {
        IList<CmdletConfigurationEntry> Cmdlets { get; }

        void AddCmdlet(CmdletConfigurationEntry entry);

        void RemoveCmdlet(CmdletConfigurationEntry entry);

        void RemoveModuleReference(string assemblyName);
    }
}
