﻿using SMAStudiovNext.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace SMAStudiovNext.Themes
{
    public class ThemeManager : IThemeManager
    {
        private const string THEMES_FOLDER = "Themes";
        private const string DEFAULT_THEME = "Github";

        private IList<Theme> _themes;
        private Theme _currentTheme = null;

        public ThemeManager()
        {
            _themes = new List<Theme>();
        }

        public void LoadThemes()
        {
            if (Directory.Exists(Path.Combine(AppHelper.CachePath, THEMES_FOLDER)))
            {
                var files = Directory.GetFiles(Path.Combine(AppHelper.CachePath, THEMES_FOLDER), "*.theme");

                foreach (var file in files)
                {
                    using (var fileReader = new StreamReader(file))
                    {
                        var serializer = new XmlSerializer(typeof(Theme));
                        var theme = (Theme)serializer.Deserialize(fileReader);

                        _themes.Add(theme);

                        if (file.EndsWith(DEFAULT_THEME + ".theme"))
                            _currentTheme = theme;
                    }
                }
            }

            if (_themes.Count == 0)
                throw new Exception("Missing theme, installation is possibly corrupt. Please reinstall.");
        }

        public Theme CurrentTheme
        {
            get { return _currentTheme; }
        }
    }
}