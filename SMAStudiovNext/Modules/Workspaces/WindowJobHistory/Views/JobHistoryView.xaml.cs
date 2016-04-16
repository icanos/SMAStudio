using System.ComponentModel;
using System.Windows.Controls;

namespace SMAStudiovNext.Modules.WindowJobHistory.Views
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
