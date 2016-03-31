using System;
using System.IO;

namespace SMAStudiovNext.Utils
{
    public class AppHelper
    {
        public static string CachePath
        {
            get
            {
                string localPathFolder = 
                    Path.Combine(
                        Environment.GetFolderPath(
                            Environment.SpecialFolder.LocalApplicationData), "SMAStudio2015");

                return localPathFolder;
            }
        }

        public static string ApplicationPath
        {
            get
            {
                return Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            }
        }

        public static string GetCustomCachePath(string segment)
        {
            return Path.Combine(CachePath, segment);
        }
    }
}
