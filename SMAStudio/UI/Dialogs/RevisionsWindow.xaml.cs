using SMAStudio.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// Interaction logic for RevisionsWindow.xaml
    /// </summary>
    public partial class RevisionsWindow : Window
    {
        public RevisionsWindow(RunbookViewModel runbookViewModel)
        {
            InitializeComponent();

            DataContext = this;

            Revisions = new ObservableCollection<RunbookVersionViewModel>();

            foreach (var version in runbookViewModel.Versions)
            {
                Revisions.Add(version);
            }

            Loaded += WindowHasLoaded;
        }

        private void WindowHasLoaded(object sender, RoutedEventArgs e)
        {
            lstRevisions.SelectionChanged += RevisionSelected;
        }

        private void RevisionSelected(object sender, SelectionChangedEventArgs e)
        {
            if (lstRevisions.SelectedItem == null)
                return;

            SelectedVersion = (RunbookVersionViewModel)lstRevisions.SelectedItem;
            DialogResult = true;
            Close();
        }

        public ObservableCollection<RunbookVersionViewModel> Revisions
        {
            get;
            set;
        }

        public RunbookVersionViewModel SelectedVersion
        {
            get;
            set;
        }
    }
}
