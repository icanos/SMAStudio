using SMAStudiovNext.Core;
using SMAStudiovNext.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace SMAStudiovNext.Themes
{
    public delegate void UpdateCurrentThemeDelegate();

    public class ThemeManager : IThemeManager
    {
        private const string THEMES_FOLDER = "Themes";
        private const string DEFAULT_THEME = "Github";

        private IList<Theme> _themes;
        private Theme _currentTheme = null;

        public event UpdateCurrentThemeDelegate UpdateCurrentTheme;

        public ThemeManager()
        {
            _themes = new List<Theme>();
        }

        public void LoadThemes()
        {
            string currentTheme = SettingsService.CurrentSettings != null && SettingsService.CurrentSettings.Theme != null ? SettingsService.CurrentSettings.Theme : DEFAULT_THEME;

            if (Directory.Exists(Path.Combine(AppHelper.CachePath, THEMES_FOLDER)))
            {
                var files = Directory.GetFiles(Path.Combine(AppHelper.CachePath, THEMES_FOLDER), "*.theme");

                foreach (var file in files)
                {
                    using (var fileReader = new StreamReader(file))
                    {
                        try
                        {
                            var serializer = new XmlSerializer(typeof(Theme));
                            var theme = (Theme)serializer.Deserialize(fileReader);

                            _themes.Add(theme);

                            if (file.EndsWith(currentTheme + ".theme"))
                                _currentTheme = theme;
                        }
                        catch (Exception)
                        {
                            // Ignore theme parse errors
                        }
                    }
                }
            }

            if (_themes.Count == 0)
                _currentTheme = new Theme
                {
                    Background = "#ffffff",
                    Foreground = "#000000",
                    Font = "Consolas",
                    FontSize = 12
                };
        }

        public void SetCurrentTheme(Theme theme)
        {
            _currentTheme = theme;

            if (UpdateCurrentTheme != null)
                UpdateCurrentTheme();
        }

        public Theme CurrentTheme
        {
            get { return _currentTheme; }
        }

        public IList<Theme> Themes
        {
            get { return _themes; }
        }
    }
}
