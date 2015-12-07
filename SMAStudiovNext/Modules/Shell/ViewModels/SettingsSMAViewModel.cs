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
using System.Windows;
using Xceed.Wpf.Toolkit;

namespace SMAStudiovNext.Modules.Shell.ViewModels
{
    //[Export(typeof(ISettingsEditor))]
    //[PartCreationPolicy(CreationPolicy.NonShared)]
    public class SettingsSMAViewModel : PropertyChangedBase, ISettingsEditor
    {
        private string _smaWebserviceUrl;
        private string _username;
        private bool _impersonateLogin;

        public SettingsSMAViewModel()
        {
            if (SettingsService.CurrentSettings != null)
            {
                //SmaWebserviceUrl = SettingsService.CurrentSettings.SmaWebserviceUrl;
                //Username = SettingsService.CurrentSettings.Username + "@" + SettingsService.CurrentSettings.Domain;
                //Impersonate = SettingsService.CurrentSettings.ImpersonatedLogin;
            }
        }
        
        public string SmaWebserviceUrl
        {
            get { return _smaWebserviceUrl; }
            set
            {
                if (value.Equals(_smaWebserviceUrl)) return;
                _smaWebserviceUrl = value;
                NotifyOfPropertyChange(() => SmaWebserviceUrl);
            }
        }

        public string Username
        {
            get { return _username; }
            set
            {
                if (value.Equals(_username)) return;
                _username = value;
                NotifyOfPropertyChange(() => Username);
            }
        }
        
        public bool Impersonate
        {
            get { return _impersonateLogin; }
            set
            {
                if (value.Equals(_impersonateLogin)) return;
                _impersonateLogin = value;
                NotifyOfPropertyChange(() => Impersonate);
            }
        }

        public string SettingsPageName
        {
            get { return "SMA"; }
        }

        public string SettingsPagePath
        {
            get { return "Connection"; }
        }

        public void ApplyChanges()
        {
            var settingsService = AppContext.Resolve<ISettingsService>();

            if (SettingsService.CurrentSettings == null)
                SettingsService.CurrentSettings = new Settings();

            //SettingsService.CurrentSettings.SmaWebserviceUrl = SmaWebserviceUrl;

            string[] usernameParts = Username.Split('@');

            if (usernameParts.Length < 2)
            {
                Xceed.Wpf.Toolkit.MessageBox.Show("Malformed username, format: username@domain.com", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            //SettingsService.CurrentSettings.Username = usernameParts[0];
            //SettingsService.CurrentSettings.Domain = usernameParts[1];

            //SettingsService.CurrentSettings.ImpersonatedLogin = Impersonate;

            // Store the settings
            settingsService.Save();
        }
    }
}
