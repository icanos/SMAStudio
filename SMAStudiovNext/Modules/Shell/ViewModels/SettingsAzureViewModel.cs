using Caliburn.Micro;
using Gemini.Modules.Settings;
using SMAStudiovNext.Core;
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
    public class SettingsAzureViewModel : PropertyChangedBase, ISettingsEditor
    {
        private string _azureKey = string.Empty;
        private string _azureUrl = string.Empty;
        private bool _enabled = false;

        public SettingsAzureViewModel()
        {
            if (SettingsService.CurrentSettings != null)
            {
                //AzureKey = SettingsService.CurrentSettings.AzureKey;
                //Enabled = SettingsService.CurrentSettings.AzureEnabled;
            }
        }

        public string AzureKey
        {
            get { return _azureKey; }
            set
            {
                if (value != null && value.Equals(_azureKey)) return;
                _azureKey = value;
                NotifyOfPropertyChange(() => AzureKey);
            }
        }

        public string AzureAutomationUrl
        {
            get { return _azureUrl; }
            set
            {
                if (value != null && value.Equals(_azureUrl)) return;
                _azureUrl = value;
                NotifyOfPropertyChange(() => AzureAutomationUrl);
            }
        }

        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                if (value.Equals(_enabled)) return;
                _enabled = value;
                NotifyOfPropertyChange(() => Enabled);
            }
        }

        public string SettingsPageName
        {
            get { return "Azure"; }
        }

        public string SettingsPagePath
        {
            get { return "Connection"; }
        }

        public void ApplyChanges()
        {
            if (SettingsService.CurrentSettings == null)
                SettingsService.CurrentSettings = new Settings();

            //SettingsService.CurrentSettings.AzureKey = AzureKey;
            //SettingsService.CurrentSettings.AzureAutomationUrl = AzureAutomationUrl;
            //SettingsService.CurrentSettings.AzureEnabled = Enabled;

            var settingsService = AppContext.Resolve<ISettingsService>();
            settingsService.Save();
        }
    }
}
