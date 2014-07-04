using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudio.Models
{
    public class PowershellProjectContent : IProjectContent
    {
        private List<CmdletConfigurationEntry> _cmdlets;
        private object _sync = new object();

        public PowershellProjectContent()
        {
            _cmdlets = new List<CmdletConfigurationEntry>();
        }

        public IList<CmdletConfigurationEntry> Cmdlets
        {
            get { return _cmdlets; }
        }

        public void AddCmdlet(CmdletConfigurationEntry entry)
        {
            lock (_sync)
            {
                if (!_cmdlets.Contains(entry))
                    _cmdlets.Add(entry);
            }
        }

        public void RemoveCmdlet(CmdletConfigurationEntry entry)
        {
            lock (_sync)
            {
                if (_cmdlets.Contains(entry))
                    _cmdlets.Remove(entry);
            }
        }

        public void RemoveModuleReference(string assemblyName)
        {
            _cmdlets.RemoveAll(c => c.PSSnapIn.AssemblyName.Equals(assemblyName, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}
