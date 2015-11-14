using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Core
{
    public class JsonConverter
    {
        public static string ToJson(object obj)
        {
            if (obj == null)
                return string.Empty;

            var powershell = PowerShell.Create();
            powershell.Runspace = RunspaceFactory.CreateRunspace();
            powershell.Runspace.Open();

            powershell.AddCommand("ConvertTo-Json").AddParameter("InputObject", obj).AddParameter("Depth", 10);

            var result = powershell.Invoke<string>().Single<string>();

            powershell.Runspace.Close();

            if (result == null)
                return string.Empty;

            return result;
        }

        public static object FromJson(string jsonText)
        {
            if (jsonText == null)
                return string.Empty;

            var powershell = PowerShell.Create();
            powershell.Runspace = RunspaceFactory.CreateRunspace();
            powershell.Runspace.Open();

            powershell.AddCommand("ConvertFrom-Json").AddParameter("InputObject", jsonText);

            var result = powershell.Invoke<object>().Single<object>();

            powershell.Runspace.Close();

            return result;
        }
    }
}
