using SMAStudiovNext.Modules.Azure.ViewModels;
using System;
using System.Windows;

namespace SMAStudiovNext.Modules.Azure.Windows
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
