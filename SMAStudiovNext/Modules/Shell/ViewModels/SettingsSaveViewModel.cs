using Caliburn.Micro;
using Gemini.Modules.Settings;
using SMAStudiovNext.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Modules.Shell.ViewModels
{
    [Export(typeof(ISettingsEditor))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class SettingsSaveViewModel : PropertyChangedBase, ISettingsEditor
    {
        public SettingsSaveViewModel()
        {
            EnableLocalCopy = SettingsService.CurrentSettings.EnableLocalCopy;
            LocalCopyPath = SettingsService.CurrentSettings.LocalCopyPath;
            AutoSaveInterval = SettingsService.CurrentSettings.AutoSaveInterval;
        }

        public string SettingsPageName
        {
            get
            {
                return "Saving";
            }
        }

        public string SettingsPagePath
        {
            get
            {
                return "Environment";
            }
        }

        public void ApplyChanges()
        {
            SettingsService.CurrentSettings.AutoSaveInterval = AutoSaveInterval;
            SettingsService.CurrentSettings.EnableLocalCopy = EnableLocalCopy;
            SettingsService.CurrentSettings.LocalCopyPath = LocalCopyPath;
        }

        public bool EnableLocalCopy
        {
            get;
            set;
        }

        public string LocalCopyPath
        {
            get;set;
        }

        public int AutoSaveInterval
        {
            get;set;
        }
    }
}
