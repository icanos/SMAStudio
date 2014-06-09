using SMAStudio.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SMAStudio.Settings
{
    public class SettingsManager : IDisposable
    {
        private static SettingsManager _instance = null;
        public static SettingsManager Current
        {
            get
            {
                if (_instance == null)
                    _instance = new SettingsManager();

                return _instance;
            }
        }

        private AppSettings _settings;

        public SettingsManager()
        {
            if (!File.Exists(Path.Combine(AppHelper.StartupPath, "settings.xml")))
            {
                _settings = new AppSettings();

                SaveSettings();
            }

            _settings = LoadSettings();
        }

        public AppSettings Settings
        {
            get { return _settings; }
            set { _settings = value; }
        }
          
        private AppSettings LoadSettings()
        {
            XmlSerializer xs = new XmlSerializer(typeof(AppSettings));
            TextReader tr = new StreamReader(Path.Combine(AppHelper.StartupPath, "settings.xml"));
            var settings = (AppSettings)xs.Deserialize(tr);

            tr.Close();

            return settings;
        }

        private void SaveSettings()
        {
            XmlSerializer xs = new XmlSerializer(typeof(AppSettings));
            TextWriter tw = new StreamWriter(Path.Combine(AppHelper.StartupPath, "settings.xml"));
            xs.Serialize(tw, _settings);

            tw.Flush();
            tw.Close();
        }

        public void Dispose()
        {
            SaveSettings();
        }
    }
}
