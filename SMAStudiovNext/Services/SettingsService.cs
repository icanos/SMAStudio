using Caliburn.Micro;
using Gemini.Framework.Services;
using SMAStudiovNext.Core;
using System.IO;
using System.Xml.Serialization;
using SMAStudiovNext.Utils;

namespace SMAStudiovNext.Services
{
    public class SettingsService : ISettingsService
    {
        private const string SETTINGS_FILE_NAME = "settings.xml";

        public SettingsService()
        {

        }

        public static Settings CurrentSettings { get; set; }

        public void Load()
        {
            if (File.Exists(Path.Combine(AppHelper.CachePath, SETTINGS_FILE_NAME)))
            {
                using (var fileReader = new StreamReader(Path.Combine(AppHelper.CachePath, SETTINGS_FILE_NAME)))
                {
                    var serializer = new XmlSerializer(typeof(Settings));
                    CurrentSettings = (Settings)serializer.Deserialize(fileReader);
                }

                var shell = IoC.Get<IShell>();
                var tools = shell.Tools;
            }
            else
                CurrentSettings = new Settings();
        }

        public void Save()
        {
            if (!Directory.Exists(AppHelper.CachePath))
                Directory.CreateDirectory(AppHelper.CachePath);

            using (var fileWriter = new StreamWriter(Path.Combine(AppHelper.CachePath, SETTINGS_FILE_NAME)))
            {
                var serializer = new XmlSerializer(typeof(Settings));
                serializer.Serialize(fileWriter, CurrentSettings);

                fileWriter.Flush();
            }
        }
    }
}
