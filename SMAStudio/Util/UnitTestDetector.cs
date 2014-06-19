using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudio.Util
{
    /// <summary>
    /// Detects if we are running a unit test.
    /// 
    /// This isn't pretty by any means, but since we have dialogs through out
    /// our application, this is needed, in order for us to skip having to click
    /// Yes every time we run a unit test. Anyone got a better idea?
    /// Please let me know!
    /// </summary>
    public static class UnitTestDetector
    {
        static UnitTestDetector()
        {
            string testAssemblyName = "Microsoft.VisualStudio.QualityTools.UnitTestFramework";
            UnitTestDetector.IsInUnitTest = AppDomain.CurrentDomain.GetAssemblies()
                .Any(a => a.FullName.StartsWith(testAssemblyName));
        }

        public static bool IsInUnitTest { get; private set; }
    }
}
