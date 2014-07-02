using SMAStudio.Models;
using SMAStudio.ViewModels;
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
    /// Interaction logic for AddNewItemDialog.xaml
    /// </summary>
    public partial class AddNewItemDialog : Window
    {
        public AddNewItemDialog()
        {
            InitializeComponent();

            DataContext = new AddNewItemViewModel();
        }

        private void ListBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var item = ItemsControl.ContainerFromElement((ListBox)sender, e.OriginalSource as DependencyObject) as ListBoxItem;
            if (item != null)
            {
                Details.DataContext = item.Content;
            }
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            if (TemplateList.SelectedItem == null)
            {
                // No template selected
                Close();
                return;
            }

            CreatedName = RunbookName.Text;

            DialogResult = true;
            Close();
        }

        public string CreatedName
        {
            get;
            set;
        }

        public DocumentTemplate SelectedTemplate
        {
            get { return (DocumentTemplate)TemplateList.SelectedItem; }
        }

        private void RunbookName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Submit as if we clicked 'Add'
                Submit_Click(sender, new RoutedEventArgs());
            }
        }
    }
}
