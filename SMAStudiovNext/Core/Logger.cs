using SMAStudiovNext.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SMAStudiovNext.Utils;

namespace SMAStudiovNext.Core
{
    public class Logger
    {
        public static void InfoFormat(string format, params object[] args)
        {
            WriteOut("INFO", format, args);
        }

        public static void Info(string message, Exception ex)
        {
            WriteOut("INFO", message, ex);
        }

        public static void ErrorFormat(string format, params object[] args)
        {
            WriteOut("ERROR", format, args);
        }

        public static void Error(string message, Exception ex)
        {
            WriteOut("ERROR", message, ex);
        }

        public static void DebugFormat(string format, params object[] args)
        {
            if (!SettingsService.CurrentSettings.Debug)
                return;

            WriteOut("DEBUG", format, args);
        }

        public static void Debug(string message, Exception ex)
        {
            if (!SettingsService.CurrentSettings.Debug)
                return;

            WriteOut("DEBUG", message, ex);
        }

        private static void WriteOut(string type, string format, params object[] args)
        {
            string logFile = AppHelper.GetCustomCachePath("SMAStudio.log");

            if (File.Exists(logFile))
            {
                var fi = new FileInfo(logFile);

                if ((fi.Length / 1024 / 1024) > 4)
                    File.Delete(logFile);
            }

            using (var textWriter = new StreamWriter(logFile, true))
            {
                if (args != null)
                    textWriter.WriteLine("[" + DateTime.Now + "] " + type.ToUpper() + ": " + string.Format(format, args));
                else
                    textWriter.WriteLine("[" + DateTime.Now + "] " + type.ToUpper() + ": " + format);

                textWriter.Flush();
            }
        }

        private static void WriteOut(string type, string message, Exception ex)
        {
            string logFile = AppHelper.GetCustomCachePath("SMAStudio.log");

            if (File.Exists(logFile))
            {
                var fi = new FileInfo(logFile);

                if ((fi.Length / 1024 / 1024) > 4)
                    File.Delete(logFile);
            }

            using (var textWriter = new StreamWriter(logFile, true))
            {
                textWriter.WriteLine("[" + DateTime.Now + "] " + type.ToUpper() + ": " + message + "\r\n" + ex.ToString());
                textWriter.Flush();
            }
        }
    }
}
