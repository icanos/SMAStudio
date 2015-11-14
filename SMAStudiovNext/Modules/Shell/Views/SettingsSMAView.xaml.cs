using SMAStudiovNext.Core;
using SMAStudiovNext.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SMAStudiovNext.Modules.Shell.Views
{
    /// <summary>
    /// Interaction logic for SMAStudioSettingsView.xaml
    /// </summary>
    public partial class SettingsSMAView : UserControl
    {
        public SettingsSMAView()
        {
            InitializeComponent();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (SettingsService.CurrentSettings == null)
                SettingsService.CurrentSettings = new Settings();

            //SettingsService.CurrentSettings.Password = DataProtection.Protect((sender as PasswordBox).Password);
        }
    }
}
