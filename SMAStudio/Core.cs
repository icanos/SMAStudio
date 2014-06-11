using SMAStudio.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudio
{
    public class Core
    {
        private static ILoggingService _instance = null;
        public static ILoggingService Log
        {
            get
            {
                if (_instance == null)
                    _instance = new log4netLoggingService();

                return _instance;
            }
        }

        public static string Version
        {
            get { return "0.0.2"; }
        }
    }
}
