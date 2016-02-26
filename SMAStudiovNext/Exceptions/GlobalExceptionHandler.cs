using System;
using System.IO;
using System.Windows;

namespace SMAStudiovNext.Core
{
    public class GlobalExceptionHandler
    {
        public static void Configure()
        {
            AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e)
            {
                var textWriter = new StreamWriter(AppHelper.GetCustomCachePath("error.log"));
                textWriter.WriteLine(e.ExceptionObject.ToString());

                textWriter.Flush();
                textWriter.Close();

                MessageBox.Show("Automation Studio crashed and the log file containing more information can be found here:\r\n" + AppHelper.GetCustomCachePath("error.log"), "Error");
            };
        }
    }
}
