using Caliburn.Micro;
using Gemini.Modules.Settings;
using SMAStudiovNext.Core;
using SMAStudiovNext.Services;
using SMAStudiovNext.Themes;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using SMAStudiovNext.Utils;

namespace SMAStudiovNext.Modules.Shell.ViewModels
{
    [Export(typeof(ISettingsEditor))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class SettingsEditorThemeViewModel : PropertyChangedBase, ISettingsEditor
    {
        private readonly IThemeManager _themeManager;
        private Theme _themeToChangeTo = null;

        public SettingsEditorThemeViewModel()
        {
            _themeManager = AppContext.Resolve<IThemeManager>();
        }

        public ObservableCollection<Theme> Themes
        {
            get { return _themeManager.Themes.ToObservableCollection(); }
        }

        public Theme CurrentTheme
        {
            get { return _themeManager.CurrentTheme; }
            set { _themeToChangeTo = value; }
        }
            
        public string SettingsPageName
        {
            get
            {
                return "Theme";
            }
        }

        public string SettingsPagePath
        {
            get
            {
                return "Editor";
            }
        }

        public void ApplyChanges()
        {
            if (_themeToChangeTo != null)
            {
                _themeManager.SetCurrentTheme(_themeToChangeTo);

                // Update the current settings to reflect our change
                SettingsService.CurrentSettings.Theme = _themeToChangeTo.Name;
            }
        }
    }
}
