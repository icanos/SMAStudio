using Caliburn.Micro;
using Gemini.Framework.Services;
using SMAStudiovNext.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using Xceed.Wpf.AvalonDock.Layout;

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

                // Load and configure all Tool sizes
                /*foreach (var toolName in CurrentSettings.ControlSizes.Keys)
                {
                    var tool = tools.FirstOrDefault(t => t.ToString().Equals(toolName));

                    if (tool == null)
                        continue;

                    if (tool.PreferredLocation == PaneLocation.Left || tool.PreferredLocation == PaneLocation.Right)
                        ((ISizableTool)tool).PreferredWidth = CurrentSettings.ControlSizes[toolName].Width;
                    else if (tool.PreferredLocation == PaneLocation.Bottom)
                        ((ISizableTool)tool).PreferredHeight = CurrentSettings.ControlSizes[toolName].Height;
                }*/
            }
        }

        public void Save()
        {
            if (!Directory.Exists(AppHelper.CachePath))
                Directory.CreateDirectory(AppHelper.CachePath);

            // Read all sizes for our tools and save that
            /*var shell = IoC.Get<IShell>();
            var tools = shell.Tools;

            CurrentSettings.ControlSizes.Clear();

            foreach (var tool in tools)
            {
                CurrentSettings.ControlSizes.Add(tool.ToString(), new Size(tool.PreferredWidth, tool.PreferredHeight));
            }*/

            using (var fileWriter = new StreamWriter(Path.Combine(AppHelper.CachePath, SETTINGS_FILE_NAME)))
            {
                var serializer = new XmlSerializer(typeof(Settings));
                serializer.Serialize(fileWriter, CurrentSettings);

                fileWriter.Flush();
            }
        }
    }
}
