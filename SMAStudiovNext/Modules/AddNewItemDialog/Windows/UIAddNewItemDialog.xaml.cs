using SMAStudiovNext.Models;
using SMAStudiovNext.Modules.AddNewItemDialog.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SMAStudiovNext.Windows
{
    /// <summary>
    /// Interaction logic for AddNewItemDialog.xaml
    /// </summary>
    public partial class UIAddNewItemDialog : Window
    {
        public UIAddNewItemDialog()
        {
            InitializeComponent();

            DataContext = new AddNewItemDialogViewModel();
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

        public RunbookTemplate SelectedTemplate
        {
            get { return (RunbookTemplate)TemplateList.SelectedItem; }
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
