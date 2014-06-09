using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudio.Logging
{
    public class log4netLoggingService : ILoggingService
    {
        private ILog _log;

        public log4netLoggingService()
        {
            _log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        }

        public void Debug(object message, Exception ex)
        {
            _log.Debug(message, ex);
        }

        public void DebugFormat(string format, params object[] args)
        {
            _log.DebugFormat(format, args);
        }

        public void Info(object message, Exception ex)
        {
            _log.Info(message, ex);
        }

        public void InfoFormat(string format, params object[] args)
        {
            _log.InfoFormat(format, args);
        }

        public void Error(object message, Exception ex)
        {
            _log.Error(message, ex);
        }

        public void ErrorFormat(string format, params object[] args)
        {
            _log.ErrorFormat(format, args);
        }

        public void Warning(object message, Exception ex)
        {
            _log.Warn(message, ex);
        }

        public void WarningFormat(string format, params object[] args)
        {
            _log.WarnFormat(format, args);
        }
    }
}
