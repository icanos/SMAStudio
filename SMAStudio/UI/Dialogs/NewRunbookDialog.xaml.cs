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
using System.Windows.Shapes;

namespace SMAStudio
{
    /// <summary>
    /// Interaction logic for NewRunbookDialog.xaml
    /// </summary>
    public partial class NewRunbookDialog : Window
    {
        public NewRunbookDialog()
        {
            InitializeComponent();

            txtRunbookName.Focus();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            RunbookName = txtRunbookName.Text;
        }

        public string RunbookName
        {
            get;
            set;
        }
    }
}
