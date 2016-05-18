using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    /// Interaction logic for SettingsSaveView.xaml
    /// </summary>
    public partial class SettingsSaveView : UserControl
    {
        public SettingsSaveView()
        {
            InitializeComponent();
        }

        private void AutoSaveIntervalPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var regex = new Regex("[^0-9.-]+"); //regex that matches disallowed text
            e.Handled = regex.IsMatch((sender as TextBox).Text);
        }
    }
}
