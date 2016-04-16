using System;
using System.Windows;
using SMAStudiovNext.Modules.DialogConnectAzure.ViewModels;

namespace SMAStudiovNext.Modules.DialogConnectAzure.Windows
{
    /// <summary>
    /// Interaction logic for AzureWindow.xaml
    /// </summary>
    public partial class AzureWindow : Window
    {
        public AzureWindow()
        {
            InitializeComponent();
            DataContext = new AzureWindowViewModel();
        }

        private void OnRefreshClicked(object sender, EventArgs e)
        {
            ((AzureWindowViewModel)DataContext).LoadCertificates();
        }
    }
}
