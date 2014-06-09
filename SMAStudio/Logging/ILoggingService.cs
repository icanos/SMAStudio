using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudio.Logging
{
    public interface ILoggingService
    {
        void Debug(object message, Exception ex);
        void DebugFormat(string format, params object[] args);
        void Info(object message, Exception ex);
        void InfoFormat(string format, params object[] args);
        void Error(object message, Exception ex);
        void ErrorFormat(string format, params object[] args);
        void Warning(object message, Exception ex);
        void WarningFormat(string format, params object[] args);
    }
}
