using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace SMAStudiovNext.Modules.JobHistory.Views
{
    /// <summary>
    /// Interaction logic for JobHistoryView.xaml
    /// </summary>
    public partial class JobHistoryView : UserControl
    {
        public JobHistoryView()
        {
            InitializeComponent();

            // Sort on start time
            var column = ResultView.Columns[2];

            // Clear current sort descriptions
            ResultView.Items.SortDescriptions.Clear();

            // Add the new sort description
            ResultView.Items.SortDescriptions.Add(new SortDescription(column.SortMemberPath, ListSortDirection.Descending));

            foreach (var col in ResultView.Columns)
            {
                col.SortDirection = null;
            }
            column.SortDirection = ListSortDirection.Descending;

            ResultView.Items.Refresh();
        }
    }
}
