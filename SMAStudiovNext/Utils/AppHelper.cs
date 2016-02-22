using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Core
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
