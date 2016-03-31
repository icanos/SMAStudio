using System;
using System.IO;
using System.Windows;
using Caliburn.Micro;
using Gemini.Modules.Output;
using SMAStudiovNext.Utils;

namespace SMAStudiovNext.Exceptions
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

        public static void Show(Exception ex)
        {
            Execute.OnUIThread(() =>
            {
                if (XmlExceptionHandler.IsXml(ex.Message))
                    XmlExceptionHandler.Show(ex.Message);
                else
                { 
                    var output = IoC.Get<IOutput>();
                    output.AppendLine("Error: " + ex.ToString());

                    MessageBox.Show(ex.Message);
                }
            });
        }
    }
}
